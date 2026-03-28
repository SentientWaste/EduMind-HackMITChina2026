using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduMindAI.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace EduMindAI.ViewModel;

public partial class HistoryViewModel : ObservableObject
{
    public readonly JsonStorageService _storageService;
    [ObservableProperty]
    private ObservableCollection<SessionItem> _todaySessions = new();

    [ObservableProperty]
    private ObservableCollection<SessionItem> _yesterdaySessions = new();

    [ObservableProperty]
    private ObservableCollection<SessionItem> _earlierSessions = new();

    [ObservableProperty]
    private string _searchKeyword = string.Empty;

    [ObservableProperty]
    private bool _isEmpty;

    private List<SessionItem> _allSessions = new();

    public event EventHandler<int>? NavigateToSessionRequested;

    public HistoryViewModel()
    {
        _storageService = new JsonStorageService();
        _ = LoadHistoryDataAsync();
    }

    private async Task LoadHistoryDataAsync()
    {
        var sessions = await _storageService.GetAllSessions();

        _allSessions = [];

        foreach (var session in sessions)
        {
            // 获取会话详情，拿到第一条用户消息
            var (_, messages) = await _storageService.GetSessionWithMessages(session.Id);

            // 找到第一条用户消息作为预览
            var firstUserMessage = messages.FirstOrDefault(m => m.IsUser)?.Content ?? "点击查看对话";

            // 截取前30个字符
            var preview = firstUserMessage.Length > 30
                ? firstUserMessage.Substring(0, 30) + "..."
                : firstUserMessage;

            _allSessions.Add(new SessionItem
            {
                Id = session.Id,
                Title = session.Title,
                Preview = preview,  // 使用第一条用户消息
                Time = session.UpdatedAt.ToString("HH:mm"),
                Date = session.UpdatedAt.Date
            });
        }

        FilterSessions(string.Empty);
    }

    partial void OnSearchKeywordChanged(string value)
    {
        FilterSessions(value);
    }

    private void FilterSessions(string keyword)
    {
        TodaySessions.Clear();
        YesterdaySessions.Clear();
        EarlierSessions.Clear();

        if (string.IsNullOrWhiteSpace(keyword))
        {
            foreach (var session in _allSessions)
            {
                AddToAppropriateList(session);
            }
        }
        else
        {
            foreach (var session in _allSessions)
            {
                if (session.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    session.Preview.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    AddToAppropriateList(session);
                }
            }
        }

        IsEmpty = !TodaySessions.Any() && !YesterdaySessions.Any() && !EarlierSessions.Any();
    }

    private void AddToAppropriateList(SessionItem session)
    {
        if (session.Date.Date == DateTime.Today.Date)
            TodaySessions.Add(session);
        else if (session.Date.Date == DateTime.Today.AddDays(-1).Date)
            YesterdaySessions.Add(session);
        else
            EarlierSessions.Add(session);
    }

    [RelayCommand]
    private async Task Export()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = desktop.MainWindow;
            if (mainWindow == null) return;

            var file = await mainWindow.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "导出历史记录",
                DefaultExtension = "json",
                SuggestedFileName = $"EduMind_History_{DateTime.Now:yyyyMMdd}.json"
            });

            if (file != null)
            {
                var path = file.Path.LocalPath;
                await _storageService.ExportToJson(path);
                System.Diagnostics.Debug.WriteLine($"导出到: {path}");
            }

        }
    }

    public void NavigateToSession(int sessionId)
    {
        NavigateToSessionRequested?.Invoke(this, sessionId);

    }

    [RelayCommand]
    public async Task DeleteSession(int sessionId)
    {
        await _storageService.DeleteSession(sessionId);
        await LoadHistoryDataAsync();
        System.Diagnostics.Debug.WriteLine($"已删除会话: {sessionId}");
    }


}
public class SessionItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Preview { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}