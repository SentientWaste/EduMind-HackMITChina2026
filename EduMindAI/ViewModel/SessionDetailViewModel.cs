using CommunityToolkit.Mvvm.ComponentModel;
using EduMindAI.ViewModel;
using System;
using System.Collections.ObjectModel;

namespace EduMind.ViewModel;

public partial class SessionDetailViewModel : ObservableObject
{
    [ObservableProperty]
    private int _sessionId;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ChatMessage> _messages = new();

    public SessionDetailViewModel(int sessionId)
    {
        SessionId = sessionId;
        LoadSessionData();
    }

    private void LoadSessionData()
    {
        Title = $"会话 #{SessionId}";

        Messages.Add(new ChatMessage
        {
            Content = "这是历史会话的对话内容",
            IsUser = true,
            Timestamp = DateTime.Now.ToString("HH:mm")
        });
        Messages.Add(new ChatMessage
        {
            Content = "你可以在这里查看完整的对话历史",
            IsUser = false,
            Timestamp = DateTime.Now.ToString("HH:mm")
        });
    }
}