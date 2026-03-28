using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using EduMindAI.Services;
using EduMindAI.ViewModel;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace EduMindAI.Views;

public partial class HomeView : UserControl
{
    private bool _isAnimating;
    private SpeechRecognitionService? _speechService;

    public HomeView()
    {
        InitializeComponent();

        DataContext = new HomeViewModel();

        // 初始化滚动事件
        if (MessageScrollViewer != null)
        {
            MessageScrollViewer.ScrollChanged += (s, e) =>
            {
                if (e.ExtentDelta.Y > 0)
                {
                    MessageScrollViewer.ScrollToEnd();
                }
            };
        }

        // 启动动态点动画
        StartDotAnimation();

        // 初始化语音服务
        try
        {
            _speechService = new SpeechRecognitionService();
            if (_speechService != null)
            {
                _speechService.SpeechRecognized += OnSpeechRecognized;
                _speechService.SpeechHypothesized += OnSpeechHypothesized;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"语音服务初始化失败: {ex.Message}");
        }
    }

    public async Task LoadSession(int sessionId)
    {
        System.Diagnostics.Debug.WriteLine($"HomeView.LoadSession({sessionId})");

        if (DataContext is HomeViewModel vm)
        {
            await vm.LoadSession(sessionId);
        }
    }
    
    private async void StartDotAnimation()
    {
        if (_isAnimating) return;
        _isAnimating = true;

        var dots = ThinkingDots;
        if (dots == null) return;

        while (_isAnimating)
        {
            if (DataContext is HomeViewModel vm && vm.IsAIThinking)
            {
                dots.Text = ".";
                await Task.Delay(400);
                dots.Text = "..";
                await Task.Delay(400);
                dots.Text = "...";
                await Task.Delay(400);
            }
            else
            {
                dots.Text = "...";
                await Task.Delay(100);
            }
        }
    }

    private void OnMessageInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            e.Handled = true;
            if (DataContext is HomeViewModel vm)
            {
                vm.SendMessageCommand.Execute(null);
            }
        }
    }

    private void OnSpeechRecognized(object? sender, string text)
    {
        if (!string.IsNullOrWhiteSpace(text) && DataContext is HomeViewModel vm)
        {
            // 通过 ViewModel 的属性添加文字
            vm.CurrentMessage += text;
            vm.IsListening = false;
            vm.ListeningStatus = "语音识别完成";
        }
    }

    private void OnSpeechHypothesized(object? sender, string text)
    {
        if (DataContext is HomeViewModel vm)
        {
            vm.ListeningStatus = $"正在听: {text}";
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _isAnimating = false;

        // 清理语音服务
        if (_speechService != null)
        {
            _speechService.SpeechRecognized -= OnSpeechRecognized;
            _speechService.SpeechHypothesized -= OnSpeechHypothesized;
            _speechService.StopListening();
        }

    }
}

// 转换器
public class BoolToAlignmentConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? Avalonia.Layout.HorizontalAlignment.Right : Avalonia.Layout.HorizontalAlignment.Left;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}