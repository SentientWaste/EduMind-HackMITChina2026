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
    private string _selectedModel;
    public ObservableCollection<string> Models { get; } = new()
    {
        "qwen3.5:9b"
    };
    public SettingViewModel()
    {
        SelectedModel = "qwen3.5:9b";
    }
}