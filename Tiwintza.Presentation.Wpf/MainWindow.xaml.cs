using System.Windows;
using Tiwintza.Presentation.Wpf.ViewModels;

namespace Tiwintza.Presentation.Wpf
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel vm)
        {
            InitializeComponent();
            DataContext = vm; // DI
        }
    }
}
