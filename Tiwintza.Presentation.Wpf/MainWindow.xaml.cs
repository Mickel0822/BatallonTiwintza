using System.Net.NetworkInformation;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Tiwintza.Presentation.Wpf.ViewModels;

namespace Tiwintza.Presentation.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainViewModel VM { get; }
        public MainWindow(AreasViewModel areasVm)
        {
            InitializeComponent();
            VM = new MainViewModel(areasVm);
            DataContext = VM;
            //Loaded += async (_, __) => await VM.AreasVM.CargarAsync();
        }

        public class MainViewModel
        {
            public AreasViewModel AreasVM { get; }
            public MainViewModel(AreasViewModel areasVm) => AreasVM = areasVm;
        }
    }
}