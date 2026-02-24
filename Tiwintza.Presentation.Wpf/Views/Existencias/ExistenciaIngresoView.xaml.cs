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
            if (sender is not TextBox textBox)
            {
                e.Handled = true;
                return;
            }

            // 1. Calcular el Texto Nuevo (simulación del resultado final)
            var textoActual = textBox.Text;
            var selStart = textBox.SelectionStart;
            var selLength = textBox.SelectionLength;
            var textoNuevo = textoActual.Substring(0, selStart) + e.Text + textoActual.Substring(selStart + selLength);

            // 2. Permitir solo dígitos, comas o puntos
            if (!Regex.IsMatch(e.Text, "^[0-9.,]+$"))
            {
                e.Handled = true;
                return;
            }

            // 3. Verificar los separadores en el TEXTO NUEVO
            // Contamos cuántos puntos o comas habría en total si permitimos esta entrada.
            int cantidadSeparadores = textoNuevo.Count(c => c == '.' || c == ',');

            // Si el resultado tendría más de un separador, bloqueamos la entrada.
            if (cantidadSeparadores > 1)
            {
                e.Handled = true;
                return;
            }

            // Si pasó todas las pruebas, permitimos la entrada.
            e.Handled = false;
        }
    }
}
