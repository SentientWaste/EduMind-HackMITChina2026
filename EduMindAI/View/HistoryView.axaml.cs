using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using EduMind.ViewModel;
using EduMindAI.ViewModel;
using FluentAvalonia.UI.Controls;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;

namespace EduMindAI;

public partial class HistoryView : UserControl
{
    public HistoryView()
    {
        InitializeComponent();

        DataContext = new HistoryViewModel();

        System.Diagnostics.Debug.WriteLine($"HistoryView 创建, DataContext={DataContext?.GetType()}");

        if (DataContext is ViewModel.HistoryViewModel vm)
        {
            vm.NavigateToSessionRequested += OnNavigateToSessionRequested;
            System.Diagnostics.Debug.WriteLine("HistoryView 订阅事件成功");
        }
    }

    private async void OnSessionClicked(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            if (sender is Border border && border.Tag is int sessionId)
            {
                var selectedSessionId = sessionId;
                System.Diagnostics.Debug.WriteLine($"1. 点击卡片，sessionId={selectedSessionId}");

                var dialog = new ContentDialog
                {
                    Title = "确认",
                    Content = "确定要加载这条记录吗？",
                    PrimaryButtonText = "确定",
                    CloseButtonText = "取消"
                };

                var result = await dialog.ShowAsync();
                System.Diagnostics.Debug.WriteLine($"2. 弹窗结果: {result}");

                if (result == ContentDialogResult.Primary)
                {
                    System.Diagnostics.Debug.WriteLine($"3. 用户点了确定");
                    if (DataContext is ViewModel.HistoryViewModel vm)
                    {
                        System.Diagnostics.Debug.WriteLine($"4. 调用 vm.NavigateToSession({selectedSessionId})");
                        vm.NavigateToSession(selectedSessionId);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"4. DataContext 不是 HistoryViewModel，而是 {DataContext?.GetType()}");
                    }
                }
            }
        }
    }

    private void OnNavigateToSessionRequested(object? sender, int sessionId)
    {
        System.Diagnostics.Debug.WriteLine($"5. HistoryView 收到事件，sessionId={sessionId}");

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.MainWindow is MainWindow mainWindow)
            {
                System.Diagnostics.Debug.WriteLine($"6. 调用 mainWindow.NavigateToSession({sessionId})");
                mainWindow.NavigateToSession(sessionId);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"6. MainWindow 为 null");
            }
        }
    }

    private async void OnDeleteSessionClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Tag is int sessionId)
        {
            var dialog = new ContentDialog
            {
                Title = "确认删除",
                Content = "确定要删除这条历史记录吗？",
                PrimaryButtonText = "删除",
                CloseButtonText = "取消"
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                if (DataContext is HistoryViewModel vm)
                {
                    await vm.DeleteSession(sessionId);
                }
            }
        }
    }
}