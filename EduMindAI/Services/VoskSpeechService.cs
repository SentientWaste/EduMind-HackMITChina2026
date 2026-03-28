using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using Vosk;
using Avalonia.Threading;

namespace EduMindAI.Services;

public class VoskSpeechService : IDisposable
{
    private Model? _model;
    private VoskRecognizer? _recognizer;
    private WaveInEvent? _waveIn;
    private Thread? _recognitionThread;
    private bool _isListening;
    private MemoryStream _audioStream;
    private readonly object _lock = new object();

    public event EventHandler<string>? SpeechRecognized;
    public event EventHandler<string>? SpeechHypothesized;

    public VoskSpeechService()
    {
        _audioStream = new MemoryStream();
        Initialize();
    }

    private void Initialize()
    {
        try
        {
            // 模型路径
            var modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "Models", "vosk-model-small-cn-0.22");

            System.Diagnostics.Debug.WriteLine($"模型路径: {modelPath}");

            if (!Directory.Exists(modelPath))
            {
                System.Diagnostics.Debug.WriteLine($"模型不存在: {modelPath}");
                return;
            }

            _model = new Model(modelPath);
            _recognizer = new VoskRecognizer(_model, 16000.0f);
            _recognizer.SetMaxAlternatives(0);
            _recognizer.SetWords(false);

            System.Diagnostics.Debug.WriteLine("Vosk 语音识别初始化成功");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Vosk 初始化失败: {ex.Message}");
        }
    }

    public void StartListening()
    {
        if (_isListening || _model == null || _recognizer == null) return;

        try
        {
            // 列出麦克风
            System.Diagnostics.Debug.WriteLine($"麦克风数量: {WaveInEvent.DeviceCount}");
            for (int i = 0; i < WaveInEvent.DeviceCount; i++)
            {
                var caps = WaveInEvent.GetCapabilities(i);
                System.Diagnostics.Debug.WriteLine($"  设备 {i}: {caps.ProductName}");
            }

            if (WaveInEvent.DeviceCount == 0)
            {
                System.Diagnostics.Debug.WriteLine("没有找到麦克风");
                return;
            }

            _waveIn = new WaveInEvent();
            _waveIn.DeviceNumber = 0;
            _waveIn.WaveFormat = new WaveFormat(16000, 16, 1);
            _waveIn.DataAvailable += OnDataAvailable;
            _waveIn.RecordingStopped += OnRecordingStopped;

            _audioStream.SetLength(0);
            _waveIn.StartRecording();
            _isListening = true;

            // 启动识别线程
            _recognitionThread = new Thread(ProcessAudio);
            _recognitionThread.Start();

            System.Diagnostics.Debug.WriteLine("开始语音识别");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"启动录音失败: {ex.Message}");
        }
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        lock (_lock)
        {
            _audioStream.Write(e.Buffer, 0, e.BytesRecorded);
        }
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("录音已停止");
    }

    private void ProcessAudio()
    {
        var buffer = new byte[4096];

        while (_isListening)
        {
            lock (_lock)
            {
                if (_audioStream.Length > 0)
                {
                    _audioStream.Position = 0;
                    var bytesRead = _audioStream.Read(buffer, 0, buffer.Length);

                    if (bytesRead > 0 && _recognizer != null)
                    {
                        if (_recognizer.AcceptWaveform(buffer, bytesRead))
                        {
                            var result = _recognizer.Result();
                            var text = ExtractTextFromResult(result);

                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                Dispatcher.UIThread.Post(() =>
                                {
                                    SpeechRecognized?.Invoke(this, text);
                                });
                            }
                        }
                        else
                        {
                            var partial = _recognizer.PartialResult();
                            var partialText = ExtractTextFromPartialResult(partial);

                            if (!string.IsNullOrWhiteSpace(partialText))
                            {
                                Dispatcher.UIThread.Post(() =>
                                {
                                    SpeechHypothesized?.Invoke(this, partialText);
                                });
                            }
                        }
                    }

                    // 移除已处理的数据
                    var remaining = _audioStream.Length - bytesRead;
                    if (remaining > 0)
                    {
                        var remainingBuffer = new byte[remaining];
                        _audioStream.Read(remainingBuffer, 0, (int)remaining);
                        _audioStream.SetLength(0);
                        _audioStream.Write(remainingBuffer, 0, remainingBuffer.Length);
                    }
                    else
                    {
                        _audioStream.SetLength(0);
                    }
                }
            }

            Thread.Sleep(50);
        }
    }

    private string ExtractTextFromResult(string result)
    {
        try
        {
            var json = System.Text.Json.JsonDocument.Parse(result);
            if (json.RootElement.TryGetProperty("text", out var textElement))
            {
                return textElement.GetString() ?? "";
            }
        }
        catch { }
        return "";
    }

    private string ExtractTextFromPartialResult(string result)
    {
        try
        {
            var json = System.Text.Json.JsonDocument.Parse(result);
            if (json.RootElement.TryGetProperty("partial", out var partialElement))
            {
                return partialElement.GetString() ?? "";
            }
        }
        catch { }
        return "";
    }

    public void StopListening()
    {
        if (!_isListening) return;

        _isListening = false;
        _waveIn?.StopRecording();
        _waveIn?.Dispose();
        _waveIn = null;

        _recognitionThread?.Join(1000);
        _recognitionThread = null;

        if (_recognizer != null)
        {
            var finalResult = _recognizer.FinalResult();
            var finalText = ExtractTextFromResult(finalResult);

            if (!string.IsNullOrWhiteSpace(finalText))
            {
                Dispatcher.UIThread.Post(() =>
                {
                    SpeechRecognized?.Invoke(this, finalText);
                });
            }
        }

        System.Diagnostics.Debug.WriteLine("停止语音识别");
    }

    public bool IsListening => _isListening;

    public void Dispose()
    {
        StopListening();
        _waveIn?.Dispose();
        _recognizer?.Dispose();
        _model?.Dispose();
        _audioStream?.Dispose();
    }
}