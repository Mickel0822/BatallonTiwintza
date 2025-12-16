using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using Tiwintza.Infrastructure.Dtos.Auditoria;

namespace Tiwintza.Presentation.Wpf.ViewModels.Auditoria;

public partial class AuditGroupItemViewModel : ObservableObject
{
    public AuditGroupItemViewModel(AuditoriaGroupedDto dto)
    {
        TransactionId = dto.TransactionId;
        FechaHora = dto.FechaHora;
        Usuario = dto.Usuario;
        AccionUsuario = dto.AccionUsuario;
        TotalOperaciones = dto.TotalOperaciones;
        
        Detalles = new ObservableCollection<AuditoriaItemModel>(
            dto.Detalles.Select(d => new AuditoriaItemModel(d)));
        
        // Auto-expand if it's a single operation or very few
        IsExpanded = dto.TotalOperaciones <= 1;
    }

    public string TransactionId { get; }
    public DateTime FechaHora { get; }
    public string Usuario { get; }
    public string AccionUsuario { get; }
    public int TotalOperaciones { get; }

    [ObservableProperty]
    private bool isExpanded;

    public ObservableCollection<AuditoriaItemModel> Detalles { get; }
}
