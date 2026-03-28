using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using EduMindAI.ViewModels;
using System;

namespace EduMindAI;

public partial class FileSummaryView : UserControl
{
    public FileSummaryView()
    {
        InitializeComponent();

        DataContext = new FileSummaryViewModel();
    }
}