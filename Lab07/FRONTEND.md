# Lumbre Biblioteca — frontend WPF

El proyecto de interfaz es `Lab07` (el nombre físico existente), con seis secciones MVVM, estilos en `Resources/Theme.xaml`, vistas en `Views/`, estado y comandos en `ViewModels/` y `Commands/`, y un adaptador de las operaciones públicas en `Services/NegocioAdapter.cs`. WPF solo referencia `Biblioteca.Negocio` y `Biblioteca.Entidades`.

## Contratos conectados en el adaptador

| Área | Operaciones públicas de Negocio |
|---|---|
| Libros | `ListarAsync`, `BuscarPorTituloOAutorAsync`, `InsertarAsync`, `ActualizarAsync`, `DarDeBajaAsync` |
| Socios | `ListarAsync`, `BuscarPorNombreODniAsync`, `InsertarAsync`, `ActualizarAsync`, `DarDeBajaAsync` |
| Préstamos y devoluciones | `RegistrarPrestamoAsync`, `ObtenerPorIdAsync`, `RegistrarDevolucionAsync` |
| Reportes | `ReportePorRangoFechasAsync` |

Las escrituras se envían de forma asíncrona a una sola operación de Negocio. La multa mostrada después de una devolución procede del valor devuelto por Negocio. La fecha límite del préstamo la fija Negocio (14 días); la interfaz la muestra como información antes de confirmar. El reporte envía fechas de calendario y la implementación existente en Datos incluye todo el día final.

## Integración pendiente del backend

La aplicación inicia con `UnavailableService` y muestra la razón del bloqueo sin datos de muestra. `LibroNegocio`, `SocioNegocio` y `PrestamoNegocio` requieren en sus constructores `AutorDatos`, `SocioDatos`, `PrestamoDatos` e `ILibroRepositorio`. WPF no puede construir esos objetos sin referenciar `Biblioteca.Datos`, lo que viola el contrato de dependencias del proyecto. Se necesita un punto de composición público fuera de WPF, preferentemente en `Biblioteca.Negocio`, que proporcione las tres instancias ya construidas. Entonces el arranque puede crear `new MainWindow(new NegocioAdapter(libros, socios, prestamos))`. Hasta que exista, las operaciones de datos no están integradas en la aplicación ejecutable.

Además, Negocio necesita exponer:

1. `ListarAutoresActivosAsync` para poblar el selector de autor de forma completa. Por ahora solo se deducen autores presentes en los libros cargados; un autor activo sin libros no se puede elegir.
2. Una consulta de libros y socios inactivos, o listas sin filtro de actividad, para que los filtros **Inactivos** y **Todos** sean completos. Las lecturas actuales en Datos devuelven solo activos.
3. Una búsqueda de préstamos pendientes por socio o DNI, y un resumen de pendientes por socio. La interfaz utiliza el reporte histórico para calcular el contador y buscar por socio; esto puede resultar costoso con muchos registros y no encuentra socios inactivos en `BuscarPorNombreODniAsync`.
4. Un resultado de consulta que incluya multa vigente por detalle si se requiere mostrarla antes de registrar la devolución. El contrato actual solo devuelve el importe al ejecutar `RegistrarDevolucionAsync`; la interfaz indica “Se calcula al devolver” y muestra `S/ 0.00` o el valor real después del éxito.

El estado visual **Vencido** se calcula a partir de la fecha límite y los detalles devueltos por Negocio. Es distinto del estado `Prestamo.Estado` almacenado. La interfaz no calcula ni cobra multas.

La navegación aplica una transición de opacidad de 160 ms cuando Windows informa que las animaciones del área de cliente están activas. Los paneles y el indicador de navegación aún cambian sin movimiento animado.

## Ejecutar y comprobar

En Windows con .NET 10 SDK, LocalDB y los scripts de `Database/` aplicados:

```powershell
dotnet build Lab07.slnx
dotnet run --project Lab07/Lab07.csproj
```

La cadena de conexión permanece en `Lab07/App.config`. Este entorno de desarrollo es macOS y no tiene `dotnet` ni WPF, por lo que no se pudo compilar ni ejecutar la ventana. Se verificó que los nueve archivos XAML son XML válido y que WPF no contiene referencias a `Biblioteca.Datos` ni llamadas bloqueantes. Quedan pendientes las pruebas en Windows de préstamo válido, límite de tres libros, sin stock, inactivos, duplicados, bajas rechazadas, devolución parcial y tardía, cierre de préstamo, escalado, teclado y carga lenta. Estas pruebas de integración dependen además del punto de composición indicado arriba.
