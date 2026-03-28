using Avalonia.Threading;
using System;
using System.Linq;
using System.Speech.Recognition;
using System.Threading;
using System.Threading.Tasks;

namespace EduMindAI.Services;

public class SpeechRecognitionService
{
    private SpeechRecognitionEngine? _recognizer;
    private bool _isListening;

    public event EventHandler<string>? SpeechRecognized;
    public event EventHandler<string>? SpeechHypothesized;

    public SpeechRecognitionService()
    {
        Initialize();
    }


    private void Initialize()
    {
        try
        {
            // 检查是否支持中文
            var cultures = SpeechRecognitionEngine.InstalledRecognizers();
            var chineseCulture = cultures.FirstOrDefault(c => c.Culture.Name.StartsWith("zh"));

            if (chineseCulture != null)
            {
                _recognizer = new SpeechRecognitionEngine(chineseCulture);
            }
            else
            {
                _recognizer = new SpeechRecognitionEngine();
            }

            // 加载自由听写语法
            _recognizer.LoadGrammar(new DictationGrammar());

            // 绑定事件
            _recognizer.SpeechRecognized += OnSpeechRecognized;
            _recognizer.SpeechHypothesized += OnSpeechHypothesized;

            System.Diagnostics.Debug.WriteLine("语音识别服务初始化成功");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"语音识别服务初始化失败: {ex.Message}");
        }
    }

    private void OnSpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
    {
        var text = e.Result.Text;
        Dispatcher.UIThread.Post(() =>
        {
            SpeechRecognized?.Invoke(this, text);
        });
    }

    private void OnSpeechHypothesized(object? sender, SpeechHypothesizedEventArgs e)
    {
        var text = e.Result.Text;
        Dispatcher.UIThread.Post(() =>
        {
            SpeechHypothesized?.Invoke(this, text);
        });
    }

    private CancellationTokenSource? _timeoutCts;

    public void StartListening()
    {
        if (_recognizer != null && !_isListening)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== 开始语音识别 ===");
                _recognizer.SetInputToDefaultAudioDevice();
                _recognizer.RecognizeAsync(RecognizeMode.Multiple);
                _isListening = true;

                // 设置 10 秒超时自动停止
                _timeoutCts?.Cancel();
                _timeoutCts = new CancellationTokenSource();
                Task.Delay(10000, _timeoutCts.Token).ContinueWith(t =>
                {
                    if (!t.IsCanceled && _isListening)
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            StopListening();
                            SpeechRecognized?.Invoke(this, "");
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"启动语音识别失败: {ex.Message}");
            }
        }
    }

    public void StopListening()
    {
        if (_recognizer != null && _isListening)
        {
            _recognizer.RecognizeAsyncStop();
            _isListening = false;
            _timeoutCts?.Cancel();
            System.Diagnostics.Debug.WriteLine("停止语音识别");
        }
    }

    public bool IsListening => _isListening;
}