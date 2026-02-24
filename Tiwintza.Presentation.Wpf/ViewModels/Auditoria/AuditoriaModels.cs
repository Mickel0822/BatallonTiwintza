using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Tiwintza.Infrastructure.Dtos.Auditoria;

namespace Tiwintza.Presentation.Wpf.ViewModels.Auditoria;

public sealed partial class AuditoriaItemModel : ObservableObject
{
    public AuditoriaItemModel(AuditoriaListItemDto dto)
    {
        Id = dto.Id;
        FechaHora = dto.FechaHora;
        Usuario = dto.Usuario;
        Accion = dto.Accion;
        Entidad = dto.Entidad;
        EntidadId = dto.EntidadId;
        Resumen = dto.Resumen;
        DetalleJson = dto.DetalleJson;
    }

    public long Id { get; }
    public DateTime FechaHora { get; }
    public string Usuario { get; }
    public string Accion { get; }
    public string Entidad { get; }
    public long? EntidadId { get; }
    public string? Resumen { get; }
    public string? DetalleJson { get; }

    public string FechaTexto => FechaHora.ToString("dd/MM/yyyy HH:mm:ss");

    public string AccionBadgeKey
        => Accion.ToLowerInvariant() switch
        {
            var s when s.Contains("crea") => "StatusSuccessBrush",
            var s when s.Contains("actual") => "StatusInfoBrush",
            var s when s.Contains("baja") || s.Contains("delete") => "StatusDangerBrush",
            var s when s.Contains("traslado") => "StatusWarningBrush",
            _ => "StatusInfoBrush"
        };
}

public sealed class SelectOption
{
    public SelectOption(string display, string? value)
    {
        Display = display;
        Value = value;
    }

    public string Display { get; }
    public string? Value { get; }

    public override string ToString() => Display;

    public static SelectOption Todos(string display) => new(display, null);
}

public sealed class AuditDetailItem
{
    public string Label { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string IconKind { get; init; } = "InformationOutline";
    public bool IsFullWidth { get; init; }
    public bool IsImportant { get; init; }
}

public sealed partial class AuditEventViewModel : ObservableObject
{
    public AuditEventViewModel(string title, string subtitle, string icon, string description, List<AuditoriaItemModel> rawItems)
    {
        Title = title;
        Subtitle = subtitle;
        IconKind = icon;
        Description = description;
        RawItems = rawItems;
    }

    public string Title { get; }
    public string Subtitle { get; }
    public string IconKind { get; }
    public string Description { get; }
    public List<AuditoriaItemModel> RawItems { get; }
    
    // Propiedades extraidas para el detalle
    public string? Observacion { get; set; }
    public ObservableCollection<AuditDetailItem> DetallePropiedades { get; } = new();
}
