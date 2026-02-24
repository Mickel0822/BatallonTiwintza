using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClosedXML.Excel;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Tiwintza.Infrastructure.Dtos.Auditoria;
using Tiwintza.Infrastructure.Services;

namespace Tiwintza.Presentation.Wpf.ViewModels.Auditoria;

public sealed partial class AuditoriaViewModel : ObservableObject
{
    private readonly IAuditoriaService _service;
    private readonly ICatalogosService _catalogosService;
    private readonly IExistenciasCrudService _existenciasCrudService;
    private bool _initialized;

    // Mapas para resolución de nombres real
    private Dictionary<string, string> _mapAreas = new();
    private Dictionary<string, string> _mapProductos = new();
    private Dictionary<string, string> _mapProveedores = new();

    public AuditoriaViewModel(IAuditoriaService service, ICatalogosService catalogosService, IExistenciasCrudService existenciasCrudService)
    {
        _service = service;
        _catalogosService = catalogosService;
        _existenciasCrudService = existenciasCrudService;

        Eventos = new ObservableCollection<AuditEventViewModel>();
        Usuarios = new ObservableCollection<SelectOption>();
        Entidades = new ObservableCollection<SelectOption>();
        Acciones = new ObservableCollection<SelectOption>();

        PageSize = 50; 
        Page = 1;
        SortBy = "fecha";
    }

    public ObservableCollection<AuditEventViewModel> Eventos { get; }
    public ObservableCollection<SelectOption> Usuarios { get; }
    public ObservableCollection<SelectOption> Entidades { get; }
    public ObservableCollection<SelectOption> Acciones { get; }

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? errorMessage;

    [ObservableProperty] private DateTime? fechaInicio;
    [ObservableProperty] private DateTime? fechaFin;
    [ObservableProperty] private SelectOption? usuarioSeleccionado;
    [ObservableProperty] private SelectOption? entidadSeleccionada;
    [ObservableProperty] private SelectOption? accionSeleccionada;
    [ObservableProperty] private string? texto;

    [ObservableProperty] private string sortBy;
    [ObservableProperty] private bool sortDesc = true;

    [ObservableProperty] private int page;
    [ObservableProperty] private int pageSize;
    [ObservableProperty] private int total;

    [ObservableProperty] private AuditEventViewModel? eventoSeleccionado;
    
    [ObservableProperty] private bool filtrosExpandido;

    [ObservableProperty] private bool isNotifyOpen;
    [ObservableProperty] private string? notifyTitle;
    [ObservableProperty] private string? notifyMessage;
    [ObservableProperty] private bool notifyIsError;

    public int PageCount => PageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling((double)Total / PageSize));

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        IsBusy = true;
        await Task.WhenAll(CargarCatalogosAsync(), CargarMapasAsync());
        await BuscarAsync();
        _initialized = true;
        IsBusy = false;
    }

    private async Task CargarMapasAsync()
    {
        try 
        {
            var tAreas = _catalogosService.ObtenerAreasAsync();
            var tProvs = _catalogosService.ObtenerProveedoresAsync();
            var tProds = _existenciasCrudService.ObtenerProductosAsync();

            await Task.WhenAll(tAreas, tProvs, tProds);

            _mapAreas = tAreas.Result.ToDictionary(k => k.Id.ToString(), v => v.Nombre);
            _mapProveedores = tProvs.Result.ToDictionary(k => k.Id.ToString(), v => v.RazonSocial);
            _mapProductos = tProds.Result.ToDictionary(k => k.Id.ToString(), v => v.Nombre);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error cargando mapas: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task BuscarAsync()
    {
        Page = 1;
        await CargarAsync();
    }

    [RelayCommand]
    private async Task RecargarAsync()
    {
        await CargarAsync();
    }

    [RelayCommand]
    private void LimpiarFiltros()
    {
        FechaInicio = null;
        FechaFin = null;
        UsuarioSeleccionado = Usuarios.FirstOrDefault();
        EntidadSeleccionada = Entidades.FirstOrDefault();
        AccionSeleccionada = Acciones.FirstOrDefault();
        Texto = null;
    }

    [RelayCommand]
    private void AlternarFiltros()
    {
        FiltrosExpandido = !FiltrosExpandido;
    }

    [RelayCommand]
    private void CerrarNotificacion() => IsNotifyOpen = false;

    [RelayCommand]
    private async Task PaginaAnteriorAsync()
    {
        if (Page <= 1) return;
        Page--;
        await CargarAsync();
    }

    [RelayCommand]
    private async Task PaginaSiguienteAsync()
    {
        if (Page >= PageCount) return;
        Page++;
        await CargarAsync();
    }

    [RelayCommand]
    private async Task CambiarOrdenAsync(string? columna)
    {
        // Simplificado: solo recargar, orden por defecto fecha
        await CargarAsync();
    }

    [RelayCommand]
    private async Task ExportarAsync()
    {
        try
        {
            ErrorMessage = null;
            IsBusy = true;
            var filtro = CrearFiltro(includePaging: false);
            var datos = await _service.ExportarAsync(filtro);
            
            if (datos.Count == 0)
            {
                MostrarNotificacion("Exportar", "No hay datos.", true);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = "Exportar auditoria",
                Filter = "Libro de Excel (*.xlsx)|*.xlsx",
                FileName = $"Auditoria_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                AddExtension = true,
                DefaultExt = ".xlsx"
            };

            if (dialog.ShowDialog() != true) return;

            // Nota: Exportamos la lista plana para Excel (más detallada)
            await Task.Run(() => ExportarExcel(dialog.FileName, datos));
            MostrarNotificacion("Completado", "Exportación exitosa.");
        }
        catch (Exception ex)
        {
            MostrarNotificacion("Error", ex.Message, true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CargarAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            EventoSeleccionado = null;

            var filtro = CrearFiltro(includePaging: true);
            // Usamos BuscarAsync plano para agrupar en Frontend
            var resultado = await _service.BuscarAsync(filtro);

            var itemsPlanos = resultado.Items.Select(x => new AuditoriaItemModel(x)).ToList();
            
            // Agrupación Inteligente (Frontend)
            var eventos = AgruparEventos(itemsPlanos);

            Eventos.Clear();
            foreach (var ev in eventos)
            {
                Eventos.Add(ev);
            }

            Total = resultado.Total;
            PageSize = resultado.PageSize;
            Page = resultado.Page;
            
            // Seleccionar primero si existe
            if (Eventos.Any()) EventoSeleccionado = Eventos.First();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            MostrarNotificacion("Error", ex.Message, true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private List<AuditEventViewModel> AgruparEventos(List<AuditoriaItemModel> items)
    {
        var result = new List<AuditEventViewModel>();
        if (!items.Any()) return result;

        var currentGroup = new List<AuditoriaItemModel>();
        AuditoriaItemModel? lastItem = null;

        // Asumimos que vienen ordenados por fecha DESC
        foreach (var item in items)
        {
            if (lastItem == null)
            {
                currentGroup.Add(item);
                lastItem = item;
                continue;
            }

            // Lógica: Mismo usuario Y Diferencia < 2 segundos
            bool sameUser = item.Usuario == lastItem.Usuario;
            var diff = (lastItem.FechaHora - item.FechaHora).Duration();
            bool closeTime = diff.TotalSeconds <= 2;

            // Opcional: Misma Entidad Principal (si TransactionId existiera usarlo)

            if (sameUser && closeTime)
            {
                currentGroup.Add(item);
            }
            else
            {
                // Cerrar grupo anterior
                result.Add(CrearEvento(currentGroup));
                currentGroup = new List<AuditoriaItemModel> { item };
                lastItem = item;
            }
        }
        
        if (currentGroup.Any())
            result.Add(CrearEvento(currentGroup));

        return result;
    }

    private AuditEventViewModel CrearEvento(List<AuditoriaItemModel> rawItems)
    {
        // 1. Determinar Tipo de Evento (Inspeccionando items)
        var mainItem = rawItems.FirstOrDefault() ?? throw new Exception("Empty Group");
        
        string title = "Evento del Sistema";
        string icon = "InformationOutline";
        string desc = "Operación registrada.";
        string dateStr = $"{mainItem.Usuario} • {mainItem.FechaHora:dd MMM HH:mm}";
        string? observacion = null;

        bool hasSalida = rawItems.Any(x => x.Entidad.Contains("salida"));
        bool hasIngreso = rawItems.Any(x => x.Entidad.Contains("ingreso"));
        bool hasActivo = rawItems.Any(x => x.Entidad.Contains("activo"));
        bool hasExistencia = rawItems.Any(x => x.Entidad.Contains("existencia"));
        
        // Función helper para buscar valores en JSONs del grupo
        string? FindValue(string key) 
        {
            foreach(var r in rawItems) 
            {
                if (string.IsNullOrEmpty(r.DetalleJson)) continue;
                try {
                    using var doc = JsonDocument.Parse(r.DetalleJson);
                    if (doc.RootElement.TryGetProperty(key, out var el)) return el.ToString();
                    // Buscar 'nombre' asociado (e.g. 'producto_nombre' si existiera)
                } catch {}
            }
            return null;
        }

        observacion = FindValue("observaciones") ?? FindValue("observacion");

        if (hasSalida)
        {
            title = "Salida de Inventario";
            icon = "Export"; 
            var cant = FindValue("cantidad") ?? "?";
            var prodId = FindValue("existencia_id") ?? FindValue("producto_id");
            var areaId = FindValue("area_id");
            var prodName = ResolverNombre("producto", prodId);
            var areaName = ResolverNombre("area", areaId);
            
            desc = $"Se retiraron {cant} unidades de {prodName} con destino a {areaName}.";
        }
        else if (hasIngreso)
        {
            title = "Ingreso de Mercadería";
            icon = "Import"; 
            var cant = FindValue("cantidad") ?? "?";
            var provId = FindValue("proveedor_id");
            var provName = ResolverNombre("proveedor", provId);
            
            desc = $"Ingreso de {cant} unidades recibidas de {provName}.";
        }
        else if (hasActivo)
        {
             // Chequear si es traslado
             if (rawItems.Any(x => x.Accion.Contains("traslado")))
             {
                 title = "Traslado de Activo";
                 icon = "SwapHorizontal";
                 var areaDest = ResolverNombre("area", FindValue("nuevo_movimiento_area_destino_id"));
                 desc = $"Activo trasladado hacia {areaDest}.";
             }
             else
             {
                 title = $"{mainItem.Accion} de Activo";
                 icon = "CubeOutline";
                 // Prioridad: nombre > codigo_inventario > descripcion
                 var name = FindValue("nombre") ?? FindValue("codigo_inventario") ?? FindValue("descripcion") ?? "(sin nombre)";
                 desc = $"Gestión del activo {name}.";
             }
        }
        else if (hasExistencia)
        {
             // Caso UPDATE existencia (Ajuste o cambio de stock manual/automático)
             title = "Movimiento de Stock";
             icon = "PackageVariantClosed";
             desc = $"Actualización de inventario por {mainItem.Usuario}.";
        }
        else
        {
            // Login o Genérico
            if (rawItems.Any(x => x.Entidad.Contains("login")))
            {
                title = "Inicio de Sesión";
                icon = "Login";
                desc = "Acceso al sistema.";
            }
            else
            {
                title = $"{mainItem.Accion} en {mainItem.Entidad}";
                desc = $"Operación sobre {rawItems.Count} registros.";
            }
        }

        var vm = new AuditEventViewModel(title, dateStr, icon, desc, rawItems)
        {
            Observacion = observacion
        };
        
        // Popular visual detail props
        PopulateDetailProps(vm, rawItems);
        
        return vm;
    }

    private void PopulateDetailProps(AuditEventViewModel vm, List<AuditoriaItemModel> items)
    {
        // Consolidar propiedades únicas importantes
        var procesados = new HashSet<string>();
        
        foreach (var item in items)
        {
            if (string.IsNullOrEmpty(item.DetalleJson)) continue;
            try
            {
                using var doc = JsonDocument.Parse(item.DetalleJson);
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (prop.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined) continue;
                    var key = prop.Name.ToLower();
                    
                    // Filtrar campos técnicos irrelevantes para el gerente
                    if (key == "id" || key == "pk" || key.StartsWith("_") || 
                        key.Contains("created") || key.Contains("creado") || 
                        key.Contains("updated") || key.Contains("actualiz") ||
                        key.Contains("usuario_id") || key.Contains("sede") || key.Contains("tenant")) continue;
                    
                    if (procesados.Contains(key)) continue;

                    string label = FormatLabel(key);
                    string val = prop.Value.ToString();
                    
                    // Humanizar
                    if (key.EndsWith("_id"))
                    {
                        var entity = key.Replace("_id", "");
                        val = ResolverNombre(entity, val); // Reemplaza ID por Nombre
                    }
                    else if (key.Contains("fecha") && DateTime.TryParse(val, out var dt))
                    {
                        val = dt.ToString("dd MMM yyyy HH:mm");
                    }
                    else if ((key.Contains("precio") || key.Contains("costo")) && decimal.TryParse(val, out var m))
                    {
                        val = $"$ {m:N2}";
                    }

                    vm.DetallePropiedades.Add(new AuditDetailItem 
                    { 
                        Label = label, 
                        Value = val, 
                        // Destacar solo lo importante
                        IsImportant = key.Contains("cantidad") || key.Contains("nombre") || key.Contains("observacion")
                    });
                    
                    procesados.Add(key);
                }
            }
            catch {}
        }
    }

    private string FormatLabel(string key)
    {
        return key switch 
        {
            "area_id" => "Área",
            "existencia_id" => "Producto",
            "proveedor_id" => "Proveedor",
            "cantidad" => "Cantidad",
            "stock_actual" => "Stock Resultante",
            _ => System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(key.Replace("_", " "))
        };
    }

    // Resolución Real de Nombres
    private string ResolverNombre(string entity, string? id)
    {
        if (string.IsNullOrEmpty(id)) return "N/A";
        
        if (entity.Contains("area"))
        {
            if (_mapAreas.TryGetValue(id, out var nombre)) return nombre;
            return $"Área #{id}";
        }
        if (entity.Contains("producto") || entity.Contains("existencia"))
        {
             if (_mapProductos.TryGetValue(id, out var nombre)) return nombre;
             return $"Producto #{id}";
        }
        if (entity.Contains("proveedor")) 
        {
            if (_mapProveedores.TryGetValue(id, out var nombre)) return nombre;
            return $"Proveedor #{id}";
        }
        
        return id;
    }

    private AuditoriaFiltroDto CrearFiltro(bool includePaging)
    {
        var page = includePaging ? (Page <= 0 ? 1 : Page) : 1;
        var size = includePaging ? (PageSize <= 0 ? 50 : PageSize) : 2000;

        return new AuditoriaFiltroDto
        {
            FechaInicio = FechaInicio?.Date,
            FechaFin = FechaFin.HasValue ? FechaFin.Value.Date.AddDays(1).AddTicks(-1) : null,
            Usuario = UsuarioSeleccionado?.Value,
            Entidad = EntidadSeleccionada?.Value,
            Accion = AccionSeleccionada?.Value,
            Texto = string.IsNullOrWhiteSpace(Texto) ? null : Texto.Trim(),
            SortBy = SortBy,
            SortDesc = SortDesc,
            Page = page,
            PageSize = size
        };
    }

    private void MostrarNotificacion(string titulo, string mensaje, bool esError = false)
    {
        NotifyTitle = titulo;
        NotifyMessage = mensaje;
        NotifyIsError = esError;
        IsNotifyOpen = true;
    }

    private async Task CargarCatalogosAsync()
    {
        try
        {
            IsBusy = true;
            var catalogos = await _service.ObtenerCatalogosAsync();

            Usuarios.Clear();
            Usuarios.Add(SelectOption.Todos("Todos los usuarios"));
            foreach (var u in catalogos.Usuarios) Usuarios.Add(new SelectOption(u, u));
            UsuarioSeleccionado = Usuarios.FirstOrDefault();

            Entidades.Clear();
            Entidades.Add(SelectOption.Todos("Todas las entidades"));
            foreach (var e in catalogos.Entidades) Entidades.Add(new SelectOption(e, e));
            EntidadSeleccionada = Entidades.FirstOrDefault();

            Acciones.Clear();
            Acciones.Add(SelectOption.Todos("Todas las acciones"));
            foreach (var a in catalogos.Acciones) Acciones.Add(new SelectOption(a, a));
            AccionSeleccionada = Acciones.FirstOrDefault();
        }
        catch (Exception ex)
        {
             // Log
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ExportarExcel(string ruta, IReadOnlyList<AuditoriaListItemDto> datos)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Auditoria");
        
        ws.Cell(1, 1).Value = "Fecha";
        ws.Cell(1, 2).Value = "Usuario";
        ws.Cell(1, 3).Value = "Evento";
        ws.Cell(1, 4).Value = "Descripción";
        ws.Cell(1, 5).Value = "Detalles";

        var headerRange = ws.Range(1, 1, 1, 5);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
        
        for (var i = 0; i < datos.Count; i++)
        {
            var r = i + 2;
            var d = datos[i];
            
            string titulo = $"{d.Accion} {d.Entidad}";
            string descripcion = "Operación registrada";
            var sbDetalles = new System.Text.StringBuilder();

            try
            {
                if (!string.IsNullOrEmpty(d.DetalleJson))
                {
                    using var doc = JsonDocument.Parse(d.DetalleJson);
                    var root = doc.RootElement;

                    // 1. Generar Narrativa Simple (Lógica simplificada de CrearEvento)
                    if (d.Entidad.Contains("salida", StringComparison.OrdinalIgnoreCase))
                    {
                        titulo = "Salida de Inventario";
                        descripcion = $"Retiro de material realizado por {d.Usuario}";
                    }
                    else if (d.Entidad.Contains("ingreso", StringComparison.OrdinalIgnoreCase))
                    {
                        titulo = "Ingreso de Mercadería";
                        descripcion = $"Recepción de material por {d.Usuario}";
                    }
                    else if (d.Entidad.Contains("activo", StringComparison.OrdinalIgnoreCase))
                    {
                        titulo = d.Accion.Contains("traslado") ? "Traslado de Activo" : $"{d.Accion} de Activo";
                        // Prioridad: nombre > codigo_inventario > descripcion
                        string activoName = "(sin nombre)";
                        if (root.TryGetProperty("nombre", out var n) && n.ValueKind == JsonValueKind.String)
                            activoName = n.GetString() ?? activoName;
                        else if (root.TryGetProperty("codigo_inventario", out var c) && c.ValueKind == JsonValueKind.String)
                            activoName = c.GetString() ?? activoName;
                        descripcion = $"Gestión del activo {activoName}";
                    }
                    else if (d.Entidad.Contains("existencia") && d.Accion.Contains("update"))
                    {
                        titulo = "Movimiento de Stock";
                        descripcion = "Ajuste de inventario";
                    }
                    else if (d.Entidad.Contains("login"))
                    {
                        titulo = "Inicio de Sesión";
                        descripcion = "Acceso al sistema";
                    }

                    // 2. Generar Detalles Key-Value (Usando Maps Reales)
                    foreach (var prop in root.EnumerateObject())
                    {
                        if (prop.Value.ValueKind == JsonValueKind.Null) continue;
                        var key = prop.Name.ToLower();

                        if (key == "id" || key == "pk" || key.StartsWith("_") || 
                            key.Contains("created") || key.Contains("creado") || 
                            key.Contains("updated") || key.Contains("actualiz") ||
                            key.Contains("usuario_id") || key.Contains("sede") || key.Contains("tenant")) continue;

                        string val = prop.Value.ToString();
                        
                        if (key.EndsWith("_id"))
                        {
                            var entity = key.Replace("_id", "");
                            val = ResolverNombre(entity, val);
                        }
                        else if (key.Contains("costo") || key.Contains("precio"))
                        {
                            if (decimal.TryParse(val, out var m)) val = $"$ {m:N2}";
                        }

                        sbDetalles.AppendLine($"{FormatLabel(key)}: {val}");
                    }
                }
            }
            catch { /* Ignorar errores de parseo en reporte */ }

            ws.Cell(r, 1).Value = d.FechaHora;
            ws.Cell(r, 2).Value = d.Usuario;
            ws.Cell(r, 3).Value = titulo;
            ws.Cell(r, 4).Value = descripcion;
            ws.Cell(r, 5).Value = sbDetalles.ToString().Trim();
            ws.Cell(r, 5).Style.Alignment.WrapText = true;
        }
        
        ws.Columns(1, 4).AdjustToContents();
        ws.Column(5).Width = 70;
        wb.SaveAs(ruta);
    }
}






