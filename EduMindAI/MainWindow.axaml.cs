using Avalonia.Controls;
using EduMindAI.ViewModel;
using EduMindAI.Views;
using EduMindIAI.ViewModel;
using FluentAvalonia.UI.Controls;
using System;

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

    [Obsolete("改为使用 Messenger")]
    public async void NavigateToSession(int sessionId) {
        // 每次都全新
        var homeVm = new HomeViewModel();
        await homeVm.LoadSession(sessionId);
        ContentFrame.Content = new HomeView { DataContext = homeVm };
    }
}