using CommunityToolkit.Mvvm.ComponentModel;

namespace EduMindIAI.ViewModel
{
    partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        public object? _currentPage;
    }
}
