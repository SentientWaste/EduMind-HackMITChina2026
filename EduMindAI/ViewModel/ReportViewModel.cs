using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduMindAI.Services;
using FluentAvalonia.UI.Controls;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EduMindAI.ViewModel;

public partial class ReportViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<ReportItem> _reports = new();

    [ObservableProperty]
    private bool _isGenerating;

    [ObservableProperty]
    private string _generatingProgress = string.Empty;

    [ObservableProperty]
    private DateTime _startDate = DateTime.Now.AddDays(-7);

    [ObservableProperty]
    private DateTime _endDate = DateTime.Now;

    [ObservableProperty]
    private string _selectedPeriod = "最近7天";

    public ObservableCollection<string> PeriodOptions { get; } = new()
    {
        "今天",
        "本周",
        "本月",
        "最近7天",
        "最近30天",
        "全部"
    };

    public bool HasNoReports => Reports.Count == 0;

    private readonly ReportService _reportService;

    public ReportViewModel()
    {
        _reportService = new ReportService();
        _ = LoadReportsAsync();

        // ✅ 初始化时设置正确的日期
        UpdateDateRange("最近7天");
    }

    // ✅ 周期变化时调用
    partial void OnSelectedPeriodChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            UpdateDateRange(value);
        }
    }

    private void UpdateDateRange(string period)
    {
        var now = DateTime.Now;
        var today = now.Date;

        switch (period)
        {
            case "今天":
                StartDate = today;
                EndDate = today;
                break;
            case "本周":
                // 本周一
                var daysToMonday = (int)today.DayOfWeek - 1;
                if (daysToMonday < 0) daysToMonday = 6;
                StartDate = today.AddDays(-daysToMonday);
                EndDate = today;
                break;
            case "本月":
                StartDate = new DateTime(today.Year, today.Month, 1);
                EndDate = today;
                break;
            case "最近7天":
                StartDate = today.AddDays(-6);
                EndDate = today;
                break;
            case "最近30天":
                StartDate = today.AddDays(-29);
                EndDate = today;
                break;
            case "全部":
                StartDate = new DateTime(2024, 1, 1);
                EndDate = today;
                break;
            default:
                StartDate = today.AddDays(-6);
                EndDate = today;
                break;
        }

        System.Diagnostics.Debug.WriteLine($"周期变化: {period}, Start={StartDate:yyyy-MM-dd}, End={EndDate:yyyy-MM-dd}");
    }

    private async Task LoadReportsAsync()
    {
        await Task.Run(() =>
        {
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var reportsPath = Path.Combine(documentsPath, "EduMind", "Reports");

            var reportFiles = new List<ReportItem>();

            if (Directory.Exists(reportsPath))
            {
                reportFiles = Directory.GetFiles(reportsPath, "EduMind_学习报告_*.docx")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .Select(f => new ReportItem
                    {
                        FileName = f.Name,
                        FilePath = f.FullName,
                        CreatedTime = f.CreationTime,
                        FileSize = GetFileSizeString(f.Length)
                    })
                    .ToList();
            }

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                Reports.Clear();
                foreach (var item in reportFiles)
                {
                    Reports.Add(item);
                }
                OnPropertyChanged(nameof(HasNoReports));
            });
        });
    }

    private string GetFileSizeString(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024} KB";
        return $"{bytes / 1024 / 1024} MB";
    }

    [RelayCommand]
    private async Task GenerateReport()
    {
        if (IsGenerating) return;

        IsGenerating = true;
        GeneratingProgress = "正在生成报告...";

        try
        {
            System.Diagnostics.Debug.WriteLine($"生成报告: Start={StartDate:yyyy-MM-dd}, End={EndDate:yyyy-MM-dd}");

            var filePath = await _reportService.GenerateWordReportAsync(StartDate, EndDate);
            await LoadReportsAsync();

            GeneratingProgress = "报告生成成功！";
            await Task.Delay(1000);

            var messageBox = MessageBoxManager.GetMessageBoxStandard(
                "生成成功",
                $"报告已保存到：\n{filePath}",
                ButtonEnum.Ok,
                Icon.Success);
            await messageBox.ShowAsync();
        }
        catch (Exception ex)
        {
            var messageBox = MessageBoxManager.GetMessageBoxStandard(
                "生成失败",
                ex.Message,
                ButtonEnum.Ok,
                Icon.Error);
            await messageBox.ShowAsync();
        }
        finally
        {
            IsGenerating = false;
            GeneratingProgress = string.Empty;
        }
    }

    [RelayCommand]
    private async Task OpenReport(ReportItem report)
    {
        if (report == null || string.IsNullOrEmpty(report.FilePath)) return;

        try
        {
            if (File.Exists(report.FilePath))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = report.FilePath,
                    UseShellExecute = true
                });
            }
            else
            {
                var messageBox = MessageBoxManager.GetMessageBoxStandard(
                    "文件不存在",
                    "报告文件已被删除，请重新生成。",
                    ButtonEnum.Ok,
                    Icon.Warning);
                await messageBox.ShowAsync();
                await LoadReportsAsync();
            }
        }
        catch (Exception ex)
        {
            var messageBox = MessageBoxManager.GetMessageBoxStandard(
                "打开失败",
                ex.Message,
                ButtonEnum.Ok,
                Icon.Error);
            await messageBox.ShowAsync();
        }
    }

    [RelayCommand]
    private async Task DeleteReport(ReportItem report)
    {
        if (report == null) return;

        var dialog = new ContentDialog
        {
            Title = "确认删除",
            Content = $"确定要删除报告「{report.FileName}」吗？",
            PrimaryButtonText = "删除",
            CloseButtonText = "取消"
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            try
            {
                if (File.Exists(report.FilePath))
                {
                    File.Delete(report.FilePath);
                }
                await LoadReportsAsync();
            }
            catch (Exception ex)
            {
                var errorDialog = new ContentDialog
                {
                    Title = "删除失败",
                    Content = ex.Message,
                    CloseButtonText = "确定"
                };
                await errorDialog.ShowAsync();
            }
        }
    }

    [RelayCommand]
    private async Task OpenFolder()
    {
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var reportsPath = Path.Combine(documentsPath, "EduMind", "Reports");

        if (!Directory.Exists(reportsPath))
        {
            Directory.CreateDirectory(reportsPath);
        }

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = reportsPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            var messageBox = MessageBoxManager.GetMessageBoxStandard(
                "打开失败",
                ex.Message,
                ButtonEnum.Ok,
                Icon.Error);
            await messageBox.ShowAsync();
        }
    }
}

public class ReportItem
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime CreatedTime { get; set; }
    public string FileSize { get; set; } = string.Empty;
}