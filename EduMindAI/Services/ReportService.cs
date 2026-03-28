using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using EduMindAI.Models;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EduMindAI.Services;

public class ReportService
{
    private readonly JsonStorageService _storageService;
    private readonly HttpClient _httpClient;
    private const string OLLAMA_URL = "http://localhost:11434/api/chat";
    private const string MODEL_NAME = "qwen3.5:9b";

    public class DailyStat
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }

    public ReportService()
    {
        _storageService = new JsonStorageService();
        _httpClient = new HttpClient();
    }

    public async Task<string> GenerateWordReportAsync(DateTime startDate, DateTime endDate)
    {
        // 获取数据
        var sessions = await _storageService.GetAllSessions();
        var endDateInclusive = endDate.AddDays(1);

        var allMessages = new List<(string content, bool isUser, DateTime time, string sessionTitle)>();

        foreach (var session in sessions)
        {
            var (_, messages) = await _storageService.GetSessionWithMessages(session.Id);
            foreach (var msg in messages)
            {
                if (msg.Timestamp >= startDate && msg.Timestamp < endDateInclusive)
                {
                    allMessages.Add((msg.Content, msg.IsUser, msg.Timestamp, session.Title));
                }
            }
        }

        if (allMessages.Count == 0)
        {
            throw new Exception($"所选周期 ({startDate:yyyy年MM月dd日} - {endDate:yyyy年MM月dd日}) 内没有学习记录");
        }

        // 统计数据
        var userMessages = allMessages.Where(m => m.isUser).ToList();
        var aiMessages = allMessages.Where(m => !m.isUser).ToList();

        // 分析用户问题类型
        var questionAnalysis = await AnalyzeUserQuestions(userMessages);

        // AI 深度分析报告
        var aiReport = await GenerateAIDeepReport(startDate, endDate, userMessages, aiMessages, questionAnalysis);

        // 生成 Word 文档
        return await CreateWordDocumentAsync(startDate, endDate, userMessages.Count, aiMessages.Count,
            allMessages.Select(m => m.sessionTitle).Distinct().Count(), aiReport, allMessages, questionAnalysis);
    }

    private async Task<QuestionAnalysis> AnalyzeUserQuestions(List<(string content, bool isUser, DateTime time, string sessionTitle)> userMessages)
    {
        if (!userMessages.Any()) return new QuestionAnalysis();

        var questions = string.Join("\n", userMessages.Take(20).Select(m => m.content));

        // ✅ 改为普通字符串拼接，不用原始字符串
        var prompt = "请分析以下用户提问，总结出：\n" +
            "1. 主要学习方向（3-5个）\n" +
            "2. 知识薄弱点（用户反复问或理解不清的地方）\n" +
            "3. 学习风格（是喜欢理论、实践、还是问题驱动？）\n" +
            "4. 用户的学习进度（入门/进阶/高级）\n\n" +
            "用户提问：\n" + questions + "\n\n" +
            "请用 JSON 格式返回，不要有其他文字：\n" +
            "{\n" +
            "    \"mainTopics\": [\"主题1\", \"主题2\"],\n" +
            "    \"weakPoints\": [\"薄弱点1\", \"薄弱点2\"],\n" +
            "    \"learningStyle\": \"学习风格描述\",\n" +
            "    \"progressLevel\": \"入门/进阶/高级\"\n" +
            "}";

        try
        {
            var result = await CallOllamaAsync(prompt);
            return JsonSerializer.Deserialize<QuestionAnalysis>(result) ?? new QuestionAnalysis();
        }
        catch
        {
            return new QuestionAnalysis
            {
                mainTopics = new List<string> { "有待进一步分析" },
                weakPoints = new List<string> { "建议多提问帮助分析" },
                learningStyle = "需要更多学习数据",
                progressLevel = "待评估"
            };
        }
    }
    private async Task<AIDeepReport> GenerateAIDeepReport(
        DateTime startDate, DateTime endDate,
        List<(string content, bool isUser, DateTime time, string sessionTitle)> userMessages,
        List<(string content, bool isUser, DateTime time, string sessionTitle)> aiMessages,
        QuestionAnalysis analysis)
    {
        // 准备对话示例
        var sampleConversations = new List<string>();
        var allMessages = userMessages.Concat(aiMessages).OrderBy(m => m.time).ToList();

        foreach (var msg in allMessages.Take(10))
        {
            var role = msg.isUser ? "用户" : "AI";
            sampleConversations.Add($"[{msg.time:HH:mm}] {role}: {msg.content.Substring(0, Math.Min(100, msg.content.Length))}");
        }

        var days = (endDate - startDate).Days + 1;
        var avgDaily = (double)allMessages.Count / days;

        var prompt = $"""
        你是一个专业的学习顾问，请根据以下学习数据生成一份详细的学习报告。

        ========== 学习数据 ==========
        学习周期：{startDate:yyyy年MM月dd日} 至 {endDate:yyyy年MM月dd日}
        学习天数：{days} 天
        总学习次数：{allMessages.Count} 次
        平均每日学习：{avgDaily:F1} 次
        用户提问：{userMessages.Count} 次
        AI 回复：{aiMessages.Count} 次

        ========== 问题分析 ==========
        主要学习方向：{string.Join("、", analysis.mainTopics)}
        知识薄弱点：{string.Join("、", analysis.weakPoints)}
        学习风格：{analysis.learningStyle}
        学习进度：{analysis.progressLevel}

        ========== 对话示例 ==========
        {string.Join("\n", sampleConversations)}

        ========== 报告要求 ==========
        请生成一份详细的学习报告，包含以下部分：

        1. 【学习概况】- 用亲切的语气总结本周学习情况
        2. 【知识点掌握分析】- 分析用户关注的知识点，哪些掌握得好，哪些需要加强
        3. 【学习风格分析】- 根据提问模式分析用户的学习习惯
        4. 【具体提升建议】- 针对薄弱点给出具体的、可操作的学习建议（至少3条）
        5. 【下一步学习方向】- 推荐接下来应该学习的具体内容

        要求：
        - 内容详实，不少于 800 字
        - 建议具体可行，不要笼统
        - 语气鼓励但专业
        - 根据实际对话内容来写
        """;

        var aiReportText = await CallOllamaAsync(prompt);

        // 提取各部分
        return ParseAIDeepReport(aiReportText);
    }

    private AIDeepReport ParseAIDeepReport(string text)
    {
        return new AIDeepReport
        {
            FullReport = text,
            Summary = ExtractSection(text, "学习概况", "知识点掌握分析"),
            KnowledgeAnalysis = ExtractSection(text, "知识点掌握分析", "学习风格分析"),
            StyleAnalysis = ExtractSection(text, "学习风格分析", "具体提升建议"),
            Suggestions = ExtractSection(text, "具体提升建议", "下一步学习方向"),
            NextSteps = ExtractSection(text, "下一步学习方向", "")
        };
    }

    private string ExtractSection(string text, string startMark, string endMark)
    {
        var start = text.IndexOf(startMark);
        if (start == -1) return "";

        start += startMark.Length;
        if (!string.IsNullOrEmpty(endMark))
        {
            var end = text.IndexOf(endMark, start);
            if (end != -1)
                return text.Substring(start, end - start).Trim();
        }
        return text.Substring(start).Trim();
    }

    private async Task<string> CallOllamaAsync(string prompt)
    {
        var requestBody = new
        {
            model = MODEL_NAME,
            messages = new[]
            {
                new { role = "system", content = "你是一个专业的学习顾问，回答详细、有深度、有建设性。" },
                new { role = "user", content = prompt }
            },
            stream = false
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(OLLAMA_URL, content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<OllamaResponse>(responseJson);

        return result?.message?.content ?? "报告生成中...";
    }

    private async Task<string> CreateWordDocumentAsync(
        DateTime startDate, DateTime endDate,
        int userCount, int aiCount, int sessionCount,
        AIDeepReport report,
        List<(string content, bool isUser, DateTime time, string sessionTitle)> messages,
        QuestionAnalysis analysis)
    {
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var reportsPath = Path.Combine(documentsPath, "EduMind", "Reports");

        if (!Directory.Exists(reportsPath))
            Directory.CreateDirectory(reportsPath);

        var baseFileName = $"EduMind_学习报告_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";
        var filePath = Path.Combine(reportsPath, $"{baseFileName}.docx");

        int counter = 1;
        while (File.Exists(filePath))
        {
            counter++;
            filePath = Path.Combine(reportsPath, $"{baseFileName}_{counter}.docx");
        }

        using (var wordDoc = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
        {
            var mainPart = wordDoc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            // 标题
            AddHeading(body, "📊 EduMind 深度学习报告", 1);
            AddParagraph(body, $"报告周期：{startDate:yyyy年MM月dd日} - {endDate:yyyy年MM月dd日}");
            AddParagraph(body, $"生成时间：{DateTime.Now:yyyy年MM月dd日 HH:mm}");
            AddEmptyLine(body);

            // 学习统计
            AddHeading(body, "一、学习数据概览", 2);
            AddStatTable(body, userCount + aiCount, userCount, aiCount, sessionCount);
            AddEmptyLine(body);

            // AI 生成的深度报告
            AddHeading(body, "二、学习概况", 2);
            AddParagraph(body, report.Summary);
            AddEmptyLine(body);

            AddHeading(body, "三、知识点掌握分析", 2);
            AddParagraph(body, report.KnowledgeAnalysis);
            AddEmptyLine(body);

            AddHeading(body, "四、学习风格分析", 2);
            AddParagraph(body, report.StyleAnalysis);
            AddEmptyLine(body);

            AddHeading(body, "五、具体提升建议", 2);
            AddParagraph(body, report.Suggestions);
            AddEmptyLine(body);

            AddHeading(body, "六、下一步学习方向", 2);
            AddParagraph(body, report.NextSteps);
            AddEmptyLine(body);

            // 详细对话记录
            AddHeading(body, "七、对话记录", 2);
            AddMessageTable(body, messages);
        }

        return filePath;
    }

    private void AddHeading(Body body, string text, int level)
    {
        var paragraph = new Paragraph();
        var run = new Run();
        run.AppendChild(new Text(text));
        paragraph.AppendChild(run);
        run.RunProperties = new RunProperties(new Bold());
        body.AppendChild(paragraph);
    }

    private void AddParagraph(Body body, string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        var paragraph = new Paragraph();
        var run = new Run();
        run.AppendChild(new Text(text));
        paragraph.AppendChild(run);
        body.AppendChild(paragraph);
    }

    private void AddEmptyLine(Body body)
    {
        var paragraph = new Paragraph();
        var run = new Run();
        run.AppendChild(new Text(""));
        paragraph.AppendChild(run);
        body.AppendChild(paragraph);
    }

    private void AddStatTable(Body body, int total, int user, int ai, int sessions)
    {
        var table = new Table();
        AddTableBorder(table);
        AddTableRow(table, "统计项目", "数值");
        AddTableRow(table, "总学习次数", total.ToString());
        AddTableRow(table, "用户提问", user.ToString());
        AddTableRow(table, "AI 回复", ai.ToString());
        AddTableRow(table, "对话会话数", sessions.ToString());
        body.AppendChild(table);
    }

    private void AddMessageTable(Body body, List<(string content, bool isUser, DateTime time, string sessionTitle)> messages)
    {
        var table = new Table();
        AddTableBorder(table);
        AddTableRow(table, "时间", "类型", "内容", "所属会话");

        foreach (var msg in messages.Take(50))
        {
            var type = msg.isUser ? "👤 用户" : "🤖 AI";
            var content = msg.content.Length > 80 ? msg.content.Substring(0, 80) + "..." : msg.content;
            AddTableRow(table, msg.time.ToString("HH:mm"), type, content, msg.sessionTitle);
        }

        if (messages.Count > 50)
        {
            AddTableRow(table, "", "", $"（共 {messages.Count} 条记录，仅显示前 50 条）", "");
        }

        body.AppendChild(table);
    }

    private void AddTableBorder(Table table)
    {
        var props = new TableProperties(
            new TableBorders(
                new TopBorder() { Val = BorderValues.Single, Size = 1 },
                new BottomBorder() { Val = BorderValues.Single, Size = 1 },
                new LeftBorder() { Val = BorderValues.Single, Size = 1 },
                new RightBorder() { Val = BorderValues.Single, Size = 1 },
                new InsideHorizontalBorder() { Val = BorderValues.Single, Size = 1 },
                new InsideVerticalBorder() { Val = BorderValues.Single, Size = 1 }
            ));
        table.AppendChild(props);
    }

    private void AddTableRow(Table table, params string[] values)
    {
        var tr = new TableRow();
        foreach (var value in values)
        {
            var tc = new TableCell();
            var para = new Paragraph();
            var run = new Run();
            run.AppendChild(new Text(value));
            para.AppendChild(run);
            tc.AppendChild(para);
            tr.AppendChild(tc);
        }
        table.AppendChild(tr);
    }
}

public class QuestionAnalysis
{
    public List<string> mainTopics { get; set; } = new();
    public List<string> weakPoints { get; set; } = new();
    public string learningStyle { get; set; } = string.Empty;
    public string progressLevel { get; set; } = string.Empty;
}

public class AIDeepReport
{
    public string FullReport { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string KnowledgeAnalysis { get; set; } = string.Empty;
    public string StyleAnalysis { get; set; } = string.Empty;
    public string Suggestions { get; set; } = string.Empty;
    public string NextSteps { get; set; } = string.Empty;
}

public class OllamaResponse
{
    [JsonPropertyName("message")]
    public OllamaResponseMessage? message { get; set; }
}

public class OllamaResponseMessage
{
    [JsonPropertyName("content")]
    public string content { get; set; } = string.Empty;
}