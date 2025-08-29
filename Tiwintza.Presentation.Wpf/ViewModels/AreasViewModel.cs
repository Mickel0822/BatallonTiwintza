using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Tiwintza.Infrastructure.Data;
using Tiwintza.Infrastructure.Data.Models;

namespace Tiwintza.Presentation.Wpf.ViewModels
{
    public partial class AreasViewModel : ObservableObject
    {
        private readonly AppDbContext _db;

        [ObservableProperty]
        private ObservableCollection<area> areas = new();

        public AreasViewModel(AppDbContext db) => _db = db;

        [RelayCommand]
        public async Task CargarAsync()
        {
            try
            {
                // 1) ¿hay conexión?
                if (!await _db.Database.CanConnectAsync())
                {
                    System.Windows.MessageBox.Show("No se puede conectar a PostgreSQL.");
                    return;
                }

                // 2) ¿hay filas?
                var total = await _db.area.CountAsync();
                if (total == 0)
                {
                    System.Windows.MessageBox.Show("No hay áreas registradas.");
                    Areas.Clear();
                    return;
                }

                // 3) cargar datos
                var list = await _db.area.AsNoTracking()
                                         .OrderBy(a => a.nombre)
                                         .ToListAsync();
                Areas = new ObservableCollection<area>(list);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al cargar áreas: {ex.Message}");
            }
        }
    }
}