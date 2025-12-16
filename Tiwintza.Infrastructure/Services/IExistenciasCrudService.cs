using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tiwintza.Infrastructure.Dtos;
using Tiwintza.Infrastructure.Dtos.Existencias;

namespace Tiwintza.Infrastructure.Services;

public interface IExistenciasCrudService
{
    Task<IReadOnlyList<ProveedorDto>> ObtenerProveedoresAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ExistenciaComboItemDto>> ObtenerProductosAsync(string? texto = null, CancellationToken ct = default);
    Task<IReadOnlyList<IdNombreDto>> ObtenerAreasAsync(CancellationToken ct = default);
    Task<ProveedorDto> CrearProveedorRapidoAsync(ProveedorCreateDto dto, CancellationToken ct = default);
    Task<ExistenciaComboItemDto> CrearExistenciaRapidaAsync(ExistenciaQuickCreateDto dto, CancellationToken ct = default);
    Task<long> RegistrarIngresoAsync(ExistenciaIngresoCreateDto dto, CancellationToken ct = default);
    Task<long> RegistrarSalidaAsync(ExistenciaSalidaCreateDto dto, CancellationToken ct = default);
}
