using CommunityToolkit.Mvvm.ComponentModel;
using Tiwintza.Infrastructure.Dtos.Catalogos;

namespace Tiwintza.Presentation.Wpf.ViewModels.Catalogos;

public sealed partial class CatalogItemModel : ObservableObject
{
    [ObservableProperty]
    private string nombre;

    public CatalogItemModel(long id, string nombre)
    {
        Id = id;
        this.nombre = nombre;
    }

    public long Id { get; }

    public CatalogItemModel Clone() => new(Id, Nombre);
}

public sealed partial class ProveedorModel : ObservableObject
{
    [ObservableProperty]
    private string ruc;

    [ObservableProperty]
    private string razonSocial;

    [ObservableProperty]
    private string? contacto;

    [ObservableProperty]
    private string? telefono;

    [ObservableProperty]
    private string? email;

    public ProveedorModel(ProveedorDetalleDto dto)
    {
        Id = dto.Id;
        ruc = dto.Ruc;
        razonSocial = dto.RazonSocial;
        contacto = dto.Contacto;
        telefono = dto.Telefono;
        email = dto.Email;
    }

    public long Id { get; }

    public void Actualizar(ProveedorDetalleDto dto)
    {
        Ruc = dto.Ruc;
        RazonSocial = dto.RazonSocial;
        Contacto = dto.Contacto;
        Telefono = dto.Telefono;
        Email = dto.Email;
    }
}

public sealed partial class ProveedorFormModel : ObservableObject
{
    [ObservableProperty]
    private long? id;

    [ObservableProperty]
    private string? ruc;

    [ObservableProperty]
    private string? razonSocial;

    [ObservableProperty]
    private string? contacto;

    [ObservableProperty]
    private string? telefono;

    [ObservableProperty]
    private string? email;

    public bool EsValido(out string? mensaje)
    {
        if (string.IsNullOrWhiteSpace(Ruc))
        {
            mensaje = "Ingrese el RUC";
            return false;
        }
        if (string.IsNullOrWhiteSpace(RazonSocial))
        {
            mensaje = "Ingrese la razon social";
            return false;
        }
        mensaje = null;
        return true;
    }

    public ProveedorUpsertDto ToDto() => new()
    {
        Ruc = Ruc!.Trim(),
        RazonSocial = RazonSocial!.Trim(),
        Contacto = string.IsNullOrWhiteSpace(Contacto) ? null : Contacto.Trim(),
        Telefono = string.IsNullOrWhiteSpace(Telefono) ? null : Telefono.Trim(),
        Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim()
    };

    public static ProveedorFormModel FromModel(ProveedorModel model) => new()
    {
        Id = model.Id,
        Ruc = model.Ruc,
        RazonSocial = model.RazonSocial,
        Contacto = model.Contacto,
        Telefono = model.Telefono,
        Email = model.Email
    };
}
