using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using EduMindAI.ViewModel;

namespace EduMindAI;

public partial class SettingView : UserControl
{
    public SettingView()
    {
        InitializeComponent();
        DataContext = new SettingViewModel();
    }
}