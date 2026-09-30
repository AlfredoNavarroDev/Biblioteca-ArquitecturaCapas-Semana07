using Biblioteca.Datos;
using Biblioteca.Entidades;

namespace Biblioteca.Negocio;

public class PrestamoNegocio
{
    private const int MaximoLibrosPendientesPorSocio = 3;
    private const int DiasPrestamoPorDefecto = 14;
    private const decimal MultaPorDiaDeAtraso = 1.50m;

    private readonly PrestamoDatos _prestamoDatos;
    private readonly SocioDatos _socioDatos;
    private readonly ILibroRepositorio _libroRepositorio;

    public PrestamoNegocio(PrestamoDatos prestamoDatos, SocioDatos socioDatos, ILibroRepositorio libroRepositorio)
    {
        _prestamoDatos = prestamoDatos;
        _socioDatos = socioDatos;
        _libroRepositorio = libroRepositorio;
    }

    public async Task<int> RegistrarPrestamoAsync(int socioId, List<int> libroIds)
    {
        if (libroIds is null || libroIds.Count == 0)
        {
            throw new ReglaNegocioException("Debe seleccionar al menos un libro para registrar el préstamo.");
        }

        var socio = await _socioDatos.ObtenerPorIdAsync(socioId)
            ?? throw new ReglaNegocioException($"No existe el socio con id {socioId}.");

        if (!socio.Activo)
        {
            throw new ReglaNegocioException($"El socio '{socio.Nombre}' no está activo.");
        }

        var pendientesActuales = await _socioDatos.ContarPrestamosPendientesAsync(socioId);
        if (pendientesActuales + libroIds.Count > MaximoLibrosPendientesPorSocio)
        {
            throw new ReglaNegocioException(
                $"El socio '{socio.Nombre}' no puede tener más de {MaximoLibrosPendientesPorSocio} libros pendientes " +
                $"(tiene {pendientesActuales} y se intentan prestar {libroIds.Count}).");
        }

        var solicitadosPorLibro = libroIds
            .GroupBy(id => id)
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var (libroId, cantidadSolicitada) in solicitadosPorLibro)
        {
            var libro = await _libroRepositorio.ObtenerPorIdAsync(libroId)
                ?? throw new ReglaNegocioException($"No existe el libro con id {libroId}.");

            if (!libro.Activo)
            {
                throw new ReglaNegocioException($"El libro '{libro.Titulo}' no está activo.");
            }

            if (libro.Ejemplares < cantidadSolicitada)
            {
                throw new ReglaNegocioException($"No hay ejemplares disponibles del libro '{libro.Titulo}'.");
            }
        }

        var fechaPrestamo = DateTime.Now;
        var fechaLimite = fechaPrestamo.AddDays(DiasPrestamoPorDefecto);

        return await _prestamoDatos.RegistrarAsync(socioId, fechaPrestamo, fechaLimite, libroIds);
    }

    public async Task<decimal> RegistrarDevolucionAsync(int prestamoId, int libroId, DateTime? fechaDevolucion = null)
    {
        var prestamo = await _prestamoDatos.ObtenerPorIdAsync(prestamoId)
            ?? throw new ReglaNegocioException($"No existe el préstamo con id {prestamoId}.");

        var detalle = prestamo.Detalles.FirstOrDefault(d => d.LibroId == libroId)
            ?? throw new ReglaNegocioException($"El préstamo {prestamoId} no incluye el libro {libroId}.");

        if (detalle.FechaDevolucion is not null)
        {
            throw new ReglaNegocioException($"El libro '{detalle.LibroTitulo}' del préstamo {prestamoId} ya fue devuelto.");
        }

        var fecha = fechaDevolucion ?? DateTime.Now;
        var multa = await _prestamoDatos.RegistrarDevolucionAsync(prestamoId, libroId, fecha, MultaPorDiaDeAtraso);
        return multa ?? 0m;
    }

    public Task<Prestamo?> ObtenerPorIdAsync(int prestamoId) => _prestamoDatos.ObtenerPorIdAsync(prestamoId);

    public Task<List<Prestamo>> ReportePorRangoFechasAsync(DateTime desde, DateTime hasta) =>
        _prestamoDatos.ReportePorRangoFechasAsync(desde, hasta);
}
