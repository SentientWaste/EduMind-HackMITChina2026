using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

namespace EduMindAI.ViewModel;

public partial class SettingViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string SelectedModel { get; set; }

    [ObservableProperty]
    public partial int SelectedThemeIndex { get; set; }

    public ObservableCollection<string> Models { get; } = new()
    {
        "qwen3.5:9b"
    };


    public ObservableCollection<string> Themes { get; } = new()
    {
        "Dark",
        "Light",
        "Default"
    };

    public SettingViewModel()
    {
        SelectedModel = "qwen3.5:9b";
    }

    partial void OnSelectedThemeIndexChanged(int value) {
        Application.Current?.RequestedThemeVariant = value switch {
            0 => ThemeVariant.Dark,
            1 => ThemeVariant.Light,
            2 => ThemeVariant.Default,
            _ => ThemeVariant.Default
        };
    }
}