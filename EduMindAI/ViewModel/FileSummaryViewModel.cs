using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Serilog.Sinks.File;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Tesseract;
using UglyToad.PdfPig;

namespace EduMindAI.ViewModels;

// Ollama 相关类
public class OllamaChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public OllamaMessage[] Messages { get; set; } = [];

    [JsonPropertyName("stream")]
    public bool Stream { get; set; }
}

public class OllamaMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

public class OllamaChatResponse
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public OllamaMessage? Message { get; set; }

    [JsonPropertyName("done")]
    public bool Done { get; set; }
}

public class OllamaStreamResponse
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public OllamaMessage? Message { get; set; }

    [JsonPropertyName("done")]
    public bool Done { get; set; }
}

public partial class FileSummaryViewModel : ObservableObject
{
    [ObservableProperty]
    private string _fileName = string.Empty;

    [ObservableProperty]
    private string _fileSize = string.Empty;

    [ObservableProperty]
    private string _summaryText = "等待文件上传...";

    [ObservableProperty]
    private bool _isFileSelected;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isSummaryReady;

    [ObservableProperty]
    private string _progressText = string.Empty;

    [ObservableProperty]
    private double _progressValue;

    [ObservableProperty]
    private bool _showProgress;

    private string? _selectedFilePath;
    private string? _fileContent;
    private readonly HttpClient _httpClient;
    private const string OLLAMA_URL = "http://localhost:11434/api/chat";
    private const string MODEL_NAME = "qwen3.5:9b";  // 改用小模型，速度快

    public FileSummaryViewModel()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(300); // 5分钟超时
        ShowProgress = false;
        ProgressValue = 0;
        ProgressText = string.Empty;
    }

    private async Task ShowError(string message)
    {
        var box = MessageBoxManager.GetMessageBoxStandard(
            "错误", message, ButtonEnum.Ok, Icon.Error);
        await box.ShowAsync();
    }

    [RelayCommand]
    private async Task PickFile()
    {
        var mainWindow = GetMainWindow();
        if (mainWindow == null) return;

        var files = await mainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择文件",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
            new FilePickerFileType("所有支持的文件")
            {
                Patterns = new[] { "*.pdf", "*.docx", "*.txt", "*.md", "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif" }
            },
            new FilePickerFileType("文档文件")
            {
                Patterns = new[] { "*.pdf", "*.docx", "*.txt", "*.md" }
            },
            new FilePickerFileType("图片文件")
            {
                Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif" }
            },
            new FilePickerFileType("所有文件")
            {
                Patterns = new[] { "*" }
            }
        }
        });

        if (files.Count > 0)
        {
            var file = files[0];
            _selectedFilePath = file.Path.LocalPath;
            var fileInfo = new FileInfo(_selectedFilePath);

            FileName = file.Name;
            FileSize = GetFileSizeString(fileInfo.Length);
            IsFileSelected = true;

            await SummarizeFile();
        }
    }

    [RelayCommand]
    private async Task CopySummary()
    {
        if (string.IsNullOrEmpty(SummaryText)) return;

        var mainWindow = GetMainWindow();
        if (mainWindow?.Clipboard != null)
        {
            await mainWindow.Clipboard.SetTextAsync(SummaryText);

            var box = MessageBoxManager.GetMessageBoxStandard(
                "提示", "已复制到剪贴板", ButtonEnum.Ok, Icon.Info);
            await box.ShowAsync();
        }
    }

    [RelayCommand]
    private async Task Resummarize()
    {
        await SummarizeFile();
    }

    private async Task SummarizeFile()
    {
        if (string.IsNullOrEmpty(_selectedFilePath)) return;

        IsLoading = true;
        ShowProgress = true;
        ProgressValue = 0;
        ProgressText = "准备读取文件...";
        SummaryText = "";
        IsSummaryReady = false;

        try
        {
            // 1. 读取文件
            ProgressValue = 10;
            ProgressText = "正在读取文件...";
            await Task.Delay(50);

            _fileContent = await ExtractFileContent(_selectedFilePath);

            if (string.IsNullOrEmpty(_fileContent))
            {
                SummaryText = "无法提取文件内容";
                return;
            }

            // 2. 内容预处理
            ProgressValue = 20;
            ProgressText = "正在分析文件内容...";
            await Task.Delay(50);

            // 限制内容长度，减少处理时间
            var preview = _fileContent.Length > 1500
                ? _fileContent.Substring(0, 1500) + "..."
                : _fileContent;

            // 3. 开始 AI 总结（流式）
            ProgressValue = 30;
            ProgressText = "AI 正在分析...";

            var summary = await CallAISummaryStreaming(FileName, preview);

            ProgressValue = 100;
            ProgressText = "总结完成！";
            await Task.Delay(100);

            SummaryText = summary;
            IsSummaryReady = true;
        }
        catch (Exception ex)
        {
            SummaryText = $"总结失败: {ex.Message}";
            ProgressText = "处理失败";
        }
        finally
        {
            IsLoading = false;
            await Task.Delay(300);
            ShowProgress = false;
        }
    }

    // 流式调用 AI
    private async Task<string> CallAISummaryStreaming(string fileName, string content)
    {
        var prompt = $"""
    请总结以下文件内容：

    文件名: {fileName}
    文件内容:
    {content}

    请用中文简洁总结要点，300字以内。
    """;

        var requestBody = new OllamaChatRequest
        {
            Model = MODEL_NAME,
            Messages = new[]
            {
            new OllamaMessage { Role = "system", Content = "你是一个专业的文件总结助手。" },
            new OllamaMessage { Role = "user", Content = prompt }
        },
            Stream = true
        };

        var json = JsonSerializer.Serialize(requestBody);
        var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, OLLAMA_URL)
        {
            Content = httpContent
        };

        try
        {
            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"HTTP 错误: {response.StatusCode}, 内容: {errorContent}");
                return $"Ollama 服务错误 ({(int)response.StatusCode}): {errorContent}";
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            var fullResponse = new StringBuilder();
            string? line;
            int chunkCount = 0;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                System.Diagnostics.Debug.WriteLine($"收到原始数据: {line}");  // 打印原始数据

                if (!string.IsNullOrWhiteSpace(line))
                {
                    try
                    {
                        var chunk = JsonSerializer.Deserialize<OllamaStreamResponse>(line);
                        if (chunk?.Message?.Content != null)
                        {
                            fullResponse.Append(chunk.Message.Content);
                            chunkCount++;
                            System.Diagnostics.Debug.WriteLine($"收到第 {chunkCount} 块: {chunk.Message.Content}");

                            // 更新进度
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                ProgressValue = Math.Min(95, 30 + chunkCount);
                                ProgressText = $"AI 正在生成总结... 已生成 {fullResponse.Length} 字";
                            });
                        }

                        if (chunk?.Done == true)
                        {
                            System.Diagnostics.Debug.WriteLine("AI 返回完成标志");
                            break;
                        }
                    }
                    catch (JsonException ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"JSON 解析错误: {ex.Message}, 原始数据: {line}");
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"总结完成，共 {fullResponse.Length} 字");
            return fullResponse.ToString();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"异常: {ex}");
            return $"无法连接到 Ollama 服务: {ex.Message}";
        }
    }
    private string ExtractImageContent(string filePath)
    {
        // 获取项目目录下的 tessdata 文件夹
        var tessDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");

        using (var engine = new TesseractEngine(tessDataPath, "chi_sim", EngineMode.Default))
        {
            using (var img = Pix.LoadFromFile(filePath))
            {
                using (var page = engine.Process(img))
                {
                    return page.GetText();
                }
            }
        }
    }

    private async Task<string> ExtractFileContent(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLower();

        return await Task.Run(() =>
        {
            return ext switch
            {
                ".txt" or ".md" => File.ReadAllText(filePath, Encoding.UTF8),
                ".docx" => ExtractWordContent(filePath),
                ".pdf" => ExtractPdfContent(filePath),
                ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" => ExtractImageContent(filePath),
                _ => $"[文件类型: {ext}]\n文件内容解析功能开发中..."
            };
        });
    }

    private string ExtractPdfContent(string filePath)
    {
        var sb = new StringBuilder();

        using (var pdf = PdfDocument.Open(filePath))
        {
            foreach (var page in pdf.GetPages())
            {
                var text = page.Text;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    sb.AppendLine(text);
                }
            }
        }

        return sb.ToString();
    }

    private string ExtractWordContent(string filePath)
    {
        var sb = new StringBuilder();

        using (var wordDoc = WordprocessingDocument.Open(filePath, false))
        {
            var body = wordDoc.MainDocumentPart?.Document.Body;
            if (body != null)
            {
                foreach (var para in body.Elements<Paragraph>())
                {
                    var text = para.InnerText;
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        sb.AppendLine(text);
                    }
                }
            }
        }

        return sb.ToString();
    }

    private string GetFileSizeString(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024} KB";
        return $"{bytes / 1024 / 1024} MB";
    }

    private Avalonia.Controls.Window? GetMainWindow()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }
        return null;
    }
}