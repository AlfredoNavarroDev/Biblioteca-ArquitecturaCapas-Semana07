using Biblioteca.Entidades;

namespace Lab07.Services;

public interface IBibliotecaService
{
    Task<List<Libro>> ListarLibrosAsync();
    Task<List<Libro>> BuscarLibrosAsync(string texto);
    Task<int> CrearLibroAsync(Libro libro);
    Task ActualizarLibroAsync(Libro libro);
    Task DarDeBajaLibroAsync(int id);
    Task<List<Socio>> ListarSociosAsync();
    Task<List<Socio>> BuscarSociosAsync(string texto);
    Task<int> CrearSocioAsync(Socio socio);
    Task ActualizarSocioAsync(Socio socio);
    Task DarDeBajaSocioAsync(int id);
    Task<int> RegistrarPrestamoAsync(int socioId, List<int> libroIds);
    Task<Prestamo?> ObtenerPrestamoAsync(int id);
    Task<decimal> RegistrarDevolucionAsync(int prestamoId, int libroId);
    Task<List<Prestamo>> ReporteAsync(DateTime desde, DateTime hasta);
}
