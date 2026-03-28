using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduMindAI.Services;
using EduMindAI.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EduMindAI.ViewModel;

public class ChatMessage {
    public string Content { get; set; } = string.Empty;
    public bool IsUser { get; set; }
    public string Timestamp { get; set; } = string.Empty;
}

public partial class HomeViewModel : ObservableObject {
    private readonly HttpClient _httpClient;
    private VoskSpeechService? _voskService;
    private readonly JsonStorageService _storageService;
    private int _currentSessionId;
    private readonly SpeechRecognitionService? _speechService;
    private const string OLLAMA_URL = "http://localhost:11434/api/chat";
    private const string MODEL_NAME = "qwen3.5:9b";

    [ObservableProperty]
    public partial ObservableCollection<ChatMessage> Messages { get; set; } = new();

    [ObservableProperty]
    public partial bool IsAIThinking { get; set; }

    [ObservableProperty]
    public partial bool IsListening { get; set; }

    [ObservableProperty]
    public partial string ListeningStatus { get; set; } = "点击麦克风开始语音输入";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    public partial string CurrentMessage { get; set; } = string.Empty;

    public bool CanSendMessageExecute => !string.IsNullOrEmpty(CurrentMessage);

    public HomeViewModel() {
        _httpClient = new HttpClient();
        _storageService = new JsonStorageService();

        // 添加欢迎消息
        Messages.Add(new ChatMessage {
            Content = "你好！我是 EduMind AI 助手，已经接入 Qwen3.5:9b 模型，有什么可以帮你的？",
            IsUser = false,
            Timestamp = GetCurrentTime()
        });

        IsAIThinking = false;

        // 初始化语音服务
        try {
            _voskService = new VoskSpeechService();
            _voskService.SpeechRecognized += OnSpeechRecognized;
            _voskService.SpeechHypothesized += OnSpeechHypothesized;
        } catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"Vosk 初始化失败: {ex.Message}");
            ListeningStatus = "语音功能不可用";
        }
    }

    [RelayCommand]
    private void ToggleListening() {
        if (_voskService == null) {
            ListeningStatus = "语音功能不可用";
            return;
        }

        if (_voskService.IsListening) {
            _voskService.StopListening();
            IsListening = false;
            ListeningStatus = "已停止";
        } else {
            _voskService.StartListening();
            IsListening = true;
            ListeningStatus = "正在听...";
        }
    }

    [RelayCommand(CanExecute = nameof(CanSendMessageExecute))]
    private async Task SendMessage() {
        if (string.IsNullOrWhiteSpace(CurrentMessage))
            return;

        // 第一条消息时创建会话
        if (_currentSessionId == 0) {
            _currentSessionId = await _storageService.CreateSession("新对话");
            System.Diagnostics.Debug.WriteLine($"创建新会话: {_currentSessionId}");
        }

        var userMessage = new ChatMessage {
            Content = CurrentMessage,
            IsUser = true,
            Timestamp = GetCurrentTime()
        };

        await Dispatcher.UIThread.InvokeAsync(() => Messages.Add(userMessage));

        var userInput = CurrentMessage;
        CurrentMessage = string.Empty;

        // 保存用户消息
        await _storageService.SaveMessage(_currentSessionId, userInput, true);

        var aiMessage = new ChatMessage {
            Content = "",
            IsUser = false,
            Timestamp = GetCurrentTime()
        };

        await Dispatcher.UIThread.InvokeAsync(() => Messages.Add(aiMessage));

        IsAIThinking = true;

        try {
            var fullResponse = new StringBuilder();

            await foreach (var chunk in CallOllamaStreamAsync(userInput)) {
                fullResponse.Append(chunk);
                await Dispatcher.UIThread.InvokeAsync(() => {
                    aiMessage.Content += chunk;
                    var temp = Messages;
                    Messages = null!;
                    Messages = temp;
                });
            }

            // 保存 AI 回复
            await _storageService.SaveMessage(_currentSessionId, fullResponse.ToString(), false);
        } catch (Exception ex) {
            await Dispatcher.UIThread.InvokeAsync(() => {
                aiMessage.Content = $"抱歉，AI 服务出错了：{ex.Message}";
                var temp = Messages;
                Messages = null!;
                Messages = temp;
            });
        } finally {
            IsAIThinking = false;
        }
    }

    [RelayCommand]
    private async static Task CopyMessage(ChatMessage message) {
        if (message == null || string.IsNullOrEmpty(message.Content))
            return;

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            var mainWindow = desktop.MainWindow;
            if (mainWindow?.Clipboard != null) {
                await mainWindow.Clipboard.SetTextAsync(message.Content);
                System.Diagnostics.Debug.WriteLine($"已复制: {message.Content.Substring(0, Math.Min(50, message.Content.Length))}");
            }
        }
    }

    private static string GetCurrentTime() {
        return DateTime.Now.ToString("HH:mm");
    }

    private void OnSpeechRecognized(object? sender, string text) {
        if (!string.IsNullOrWhiteSpace(text)) {
            CurrentMessage += text;
            IsListening = false;
            ListeningStatus = "识别完成";
            _voskService?.StopListening();
        }
    }

    private void OnSpeechHypothesized(object? sender, string text) {
        ListeningStatus = $"正在听: {text}";
    }

    public async Task LoadSession(int sessionId) {
        System.Diagnostics.Debug.WriteLine($"HomeViewModel.LoadSession({sessionId})");

        var (session, messages) = await Task.Run(() => _storageService.GetSessionWithMessages(sessionId));

        Messages.Clear();
        foreach (var msg in messages) {
            Messages.Add(new ChatMessage {
                Content = msg.Content,
                IsUser = msg.IsUser,
                Timestamp = msg.Timestamp.ToString("HH:mm")
            });
        }

        _currentSessionId = sessionId;
        System.Diagnostics.Debug.WriteLine($"加载完成，Messages 数量: {Messages.Count}");
    }

    private async IAsyncEnumerable<string> CallOllamaStreamAsync(string prompt) {
        var requestBody = new OllamaChatRequest {
            Model = MODEL_NAME,
            Messages = new[]
            {
                new OllamaMessage { Role = "system", Content = "你是一个有帮助的教育助手，名叫 EduMind。并且你返回的内容不能说markdown" },
                new OllamaMessage { Role = "user", Content = prompt }
            },
            Stream = true
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, OLLAMA_URL) {
            Content = content
        };

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync()) != null) {
            if (!string.IsNullOrWhiteSpace(line)) {
                OllamaStreamResponse? chunk = null;
                try {
                    chunk = JsonSerializer.Deserialize<OllamaStreamResponse>(line);
                } catch {
                    // 忽略解析错误
                }

                if (chunk?.Message?.Content != null) {
                    yield return chunk.Message.Content;
                }
            }
        }
    }
}