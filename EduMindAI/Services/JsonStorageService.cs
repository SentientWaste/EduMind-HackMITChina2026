// Services/JsonStorageService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace EduMindAI.Services;

public class JsonStorageService
{
    private readonly string _historyPath;
    private readonly string _indexPath;
    private List<SessionInfo> _sessions;

    public JsonStorageService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var eduMindPath = Path.Combine(appData, "EduMind");
        _historyPath = Path.Combine(eduMindPath, "history");

        Directory.CreateDirectory(_historyPath);

        _indexPath = Path.Combine(_historyPath, "_index.json");
        _sessions = LoadIndex();
    }

    // 改为 public
    public class SessionInfo
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int MessageCount { get; set; }
    }

    // 改为 public
    public class MessageData
    {
        public string Content { get; set; } = string.Empty;
        public bool IsUser { get; set; }
        public DateTime Timestamp { get; set; }
    }

    private class SessionDetail
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<MessageData> Messages { get; set; } = new();
    }

    private List<SessionInfo> LoadIndex()
    {
        if (File.Exists(_indexPath))
        {
            try
            {
                var json = File.ReadAllText(_indexPath);
                return JsonSerializer.Deserialize<List<SessionInfo>>(json) ?? new List<SessionInfo>();
            }
            catch
            {
                return new List<SessionInfo>();
            }
        }
        return new List<SessionInfo>();
    }

    private void SaveIndex()
    {
        var json = JsonSerializer.Serialize(_sessions, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_indexPath, json);
    }

    private string GetSessionFilePath(int sessionId) => Path.Combine(_historyPath, $"session_{sessionId}.json");

    // 创建新会话
    public async Task<int> CreateSession(string title = "新对话")
    {
        var newId = _sessions.Count > 0 ? _sessions.Max(s => s.Id) + 1 : 1;

        var sessionInfo = new SessionInfo
        {
            Id = newId,
            Title = title,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            MessageCount = 0
        };

        _sessions.Add(sessionInfo);
        SaveIndex();

        var sessionDetail = new SessionDetail
        {
            Id = newId,
            Title = title,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            Messages = new List<MessageData>()
        };

        var json = JsonSerializer.Serialize(sessionDetail, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(GetSessionFilePath(newId), json);

        return newId;
    }

    // 保存消息
    public async Task SaveMessage(int sessionId, string content, bool isUser)
    {
        var sessionInfo = _sessions.FirstOrDefault(s => s.Id == sessionId);
        if (sessionInfo == null) return;

        var filePath = GetSessionFilePath(sessionId);
        SessionDetail sessionDetail;

        if (File.Exists(filePath))
        {
            var json = await File.ReadAllTextAsync(filePath);
            sessionDetail = JsonSerializer.Deserialize<SessionDetail>(json) ?? new SessionDetail { Id = sessionId };
        }
        else
        {
            sessionDetail = new SessionDetail { Id = sessionId, Title = sessionInfo.Title };
        }

        sessionDetail.Messages.Add(new MessageData
        {
            Content = content,
            IsUser = isUser,
            Timestamp = DateTime.Now
        });
        sessionDetail.UpdatedAt = DateTime.Now;

        var newJson = JsonSerializer.Serialize(sessionDetail, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, newJson);

        sessionInfo.UpdatedAt = DateTime.Now;
        sessionInfo.MessageCount = sessionDetail.Messages.Count;

        // 第一条用户消息作为标题
        if (sessionDetail.Messages.Count == 1 && sessionDetail.Messages[0].IsUser)
        {
            var firstMsg = sessionDetail.Messages[0].Content;
            sessionInfo.Title = firstMsg.Length > 30 ? firstMsg.Substring(0, 30) + "..." : firstMsg;
            sessionDetail.Title = sessionInfo.Title;
            await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(sessionDetail, new JsonSerializerOptions { WriteIndented = true }));
        }

        SaveIndex();
    }

    // 获取所有会话
    public async Task<List<SessionInfo>> GetAllSessions()
    {
        return await Task.FromResult(_sessions.OrderByDescending(s => s.UpdatedAt).ToList());
    }

    // 获取会话详情
    public async Task<(SessionInfo? session, List<MessageData> messages)> GetSessionWithMessages(int sessionId)
    {
        var sessionInfo = _sessions.FirstOrDefault(s => s.Id == sessionId);
        if (sessionInfo == null) return (null, new List<MessageData>());

        var filePath = GetSessionFilePath(sessionId);
        if (File.Exists(filePath))
        {
            var json = await File.ReadAllTextAsync(filePath);
            var sessionDetail = JsonSerializer.Deserialize<SessionDetail>(json);
            if (sessionDetail != null)
            {
                return (sessionInfo, sessionDetail.Messages);
            }
        }

        return (sessionInfo, new List<MessageData>());
    }

    // 删除会话
    public async Task DeleteSession(int sessionId)
    {
        var session = _sessions.FirstOrDefault(s => s.Id == sessionId);
        if (session != null)
        {
            _sessions.Remove(session);
            SaveIndex();

            var filePath = GetSessionFilePath(sessionId);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        await Task.CompletedTask;
    }

    // 在 JsonStorageService.cs 中添加这个方法
    public async Task ExportToJson(string filePath)
    {
        var allData = new List<object>();
        foreach (var sessionInfo in _sessions)
        {
            var filePathSession = GetSessionFilePath(sessionInfo.Id);
            if (File.Exists(filePathSession))
            {
                var json = await File.ReadAllTextAsync(filePathSession);
                var sessionDetail = JsonSerializer.Deserialize<SessionDetail>(json);
                allData.Add(new { Info = sessionInfo, Detail = sessionDetail });
            }
        }

        var exportJson = JsonSerializer.Serialize(allData, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, exportJson);
    }

    // 导出所有会话
    public async Task ExportAll(string exportPath)
    {
        var allData = new List<object>();
        foreach (var sessionInfo in _sessions)
        {
            var filePath = GetSessionFilePath(sessionInfo.Id);
            if (File.Exists(filePath))
            {
                var json = await File.ReadAllTextAsync(filePath);
                var sessionDetail = JsonSerializer.Deserialize<SessionDetail>(json);
                allData.Add(new { Info = sessionInfo, Detail = sessionDetail });
            }
        }

        var exportJson = JsonSerializer.Serialize(allData, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(exportPath, exportJson);
    }
}