using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using EduMindAI.ViewModel;
using EduMindAI.Views;
using EduMindIAI.ViewModel;
using FluentAvalonia.UI.Controls;
using System;
using System.Threading.Tasks;

namespace EduMindAI;

public partial class MainWindow : Window {
    public MainWindow() {
        InitializeComponent();
        ContentFrame.Navigate(typeof(HomeView));
        DataContext = new MainWindowViewModel();
    }

    private void OnNavigationViewItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e) {
        if (e.InvokedItemContainer is NavigationViewItem item && item.Tag is string tag) {
            if (tag == "Home")
                ContentFrame.Navigate(typeof(HomeView));
            else if (tag == "History")
                ContentFrame.Navigate(typeof(HistoryView));
            else if (tag == "Setting")
                ContentFrame.Navigate(typeof(SettingView));
            else if (tag == "FileSummary")
                ContentFrame.Navigate(typeof(FileSummaryView));
            else if (tag == "Report")
                ContentFrame.Navigate(typeof(ReportView));
        }
    }

    public async void NavigateToSession(int sessionId)
    {
        System.Diagnostics.Debug.WriteLine($"NavigateToSession 开始: {sessionId}");

        var loadingView = new TextBlock
        {
            Text = "加载中...",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            FontSize = 16,
            Foreground = Brushes.Gray
        };
        ContentFrame.Content = loadingView;

        var homeVm = new HomeViewModel();
        await Task.Run(async () => await homeVm.LoadSession(sessionId));

        var homeView = new HomeView { DataContext = homeVm };
        ContentFrame.Content = homeView;

        System.Diagnostics.Debug.WriteLine($"NavigateToSession 完成");
    }
}