using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tiwintza.Infrastructure.Dtos.Catalogos;

namespace Tiwintza.Infrastructure.Services;

public interface ICatalogosService
{
    // Áreas
    Task<IReadOnlyList<CatalogoItemDto>> ObtenerAreasAsync(CancellationToken ct = default);
    Task<CatalogoItemDto> CrearAreaAsync(string nombre, CancellationToken ct = default);
    Task<CatalogoItemDto> ActualizarAreaAsync(long id, string nombre, CancellationToken ct = default);
    Task EliminarAreaAsync(long id, CancellationToken ct = default);

    // Tipos
    Task<IReadOnlyList<CatalogoItemDto>> ObtenerTiposAsync(CancellationToken ct = default);
    Task<CatalogoItemDto> CrearTipoAsync(string nombre, CancellationToken ct = default);
    Task<CatalogoItemDto> ActualizarTipoAsync(long id, string nombre, CancellationToken ct = default);
    Task EliminarTipoAsync(long id, CancellationToken ct = default);

    // Proveedores
    Task<IReadOnlyList<ProveedorDetalleDto>> ObtenerProveedoresAsync(CancellationToken ct = default);
    Task<ProveedorDetalleDto> CrearProveedorAsync(ProveedorUpsertDto dto, CancellationToken ct = default);
    Task<ProveedorDetalleDto> ActualizarProveedorAsync(long id, ProveedorUpsertDto dto, CancellationToken ct = default);
    Task EliminarProveedorAsync(long id, CancellationToken ct = default);
}
