// Models/ChatData.cs
using System;
using System.Collections.Generic;

namespace EduMindAI.Models;

public class ChatData
{
    public List<SessionData> Sessions { get; set; } = [];
}

public class SessionData
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<MessageData> Messages { get; set; } = [];
}

public class MessageData
{
    public string Content { get; set; } = string.Empty;
    public bool IsUser { get; set; }
    public DateTime Timestamp { get; set; }
}