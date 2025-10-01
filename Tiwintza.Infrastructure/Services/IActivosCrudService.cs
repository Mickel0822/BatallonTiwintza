using System.Collections.Generic;
using System.Threading.Tasks;
using Tiwintza.Infrastructure.Dtos;

namespace Tiwintza.Infrastructure.Services;

public interface IActivosCrudService
{
    Task<(IEnumerable<IdNombreDto> Areas,
          IEnumerable<IdNombreDto> Estados,
          IEnumerable<IdNombreDto> Tipos,
          IEnumerable<ProveedorDto> Proveedores)> CatalogosFormAsync();
    Task<IReadOnlyList<ActivoMovimientoDto>> MovimientosAsync(long activoId);
    Task<ProveedorDto> CrearProveedorRapidoAsync(ProveedorCreateDto dto);

    Task<ActivoFormDto> ObtenerAsync(long id);

    Task<long> CrearAsync(ActivoCreateDto dto);

    Task ActualizarAsync(long id, ActivoUpdateDto dto);

    // Movimientos
    Task TrasladarAsync(long activoId, long areaDestinoId, DateOnly fecha, string? observacion = null, string? usuario = null);

    Task DarBajaAsync(long activoId, string codigoInformeTecnico, DateOnly fechaBaja, string responsable, string? observaciones = null);
}
