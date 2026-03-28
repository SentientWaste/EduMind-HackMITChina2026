using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using EduMindAI.ViewModel;
using System;

namespace EduMindAI.Views;

public partial class ReportView : UserControl
{
    public ReportView()
    {
        InitializeComponent();

        DataContext = new ReportViewModel();
    }

    private void ForceRefresh()
    {
        var vm = DataContext;
        DataContext = null;
        DataContext = vm;
        InvalidateVisual();
    }

    private void SetBrushColor(string key, string colorHex)
    {
        if (Resources[key] is SolidColorBrush brush)
        {
            brush.Color = Color.Parse(colorHex);
        }
        else
        {
            Resources[key] = new SolidColorBrush(Color.Parse(colorHex));
        }
    }
}