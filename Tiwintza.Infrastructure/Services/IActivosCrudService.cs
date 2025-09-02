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

    Task<ActivoFormDto> ObtenerAsync(long id);

    Task<long> CrearAsync(ActivoCreateDto dto);

    Task ActualizarAsync(long id, ActivoUpdateDto dto);
}
