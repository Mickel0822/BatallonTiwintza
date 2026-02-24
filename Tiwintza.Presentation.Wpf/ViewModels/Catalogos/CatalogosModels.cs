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

        var rucTrim = Ruc.Trim();
        if (rucTrim.Length != 10 && rucTrim.Length != 13)
        {
            mensaje = "El RUC/Cédula debe tener 10 o 13 dígitos";
            return false;
        }

        if (!long.TryParse(rucTrim, out _))
        {
            mensaje = "El RUC/Cédula debe contener solo números";
            return false;
        }

        if (string.IsNullOrWhiteSpace(RazonSocial))
        {
            mensaje = "Ingrese la razón social";
            return false;
        }

        if (RazonSocial.Length > 100)
        {
            mensaje = "La Razón Social excede los 100 caracteres";
            return false;
        }

        if (Contacto?.Length > 100)
        {
            mensaje = "El Contacto excede los 100 caracteres";
            return false;
        }

        if (Telefono?.Length > 15) // DB likely 15 or 20
        {
            mensaje = "El Teléfono excede los 15 caracteres";
            return false;
        }

        if (Email?.Length > 100)
        {
            mensaje = "El Email excede los 100 caracteres";
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
