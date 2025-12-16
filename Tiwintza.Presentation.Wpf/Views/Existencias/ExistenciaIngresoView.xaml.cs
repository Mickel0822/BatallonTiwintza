using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace Tiwintza.Presentation.Wpf.Views.Existencias
{
    public partial class ExistenciaIngresoView : UserControl
    {
        public ExistenciaIngresoView()
        {
            InitializeComponent();
        }

        // Permite solo números enteros (0-9)
        private void ValidarSoloEnteros(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        // Permite números y un solo punto o coma decimal
        private void ValidarDecimales(object sender, TextCompositionEventArgs e)
        {
            // Acepta dígitos y punto o coma según tu configuración regional
            Regex regex = new Regex("[^0-9,.]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}
