using Biblioteca.Entidades;

namespace Lab07.Services;

public sealed class UnavailableService : IBibliotecaService
{
    private static InvalidOperationException Missing() => new(
        "Falta el punto de composición de Negocio. El backend debe proporcionar las instancias de LibroNegocio, SocioNegocio y PrestamoNegocio sin que WPF acceda a Datos.");
    public Task<List<Libro>> ListarLibrosAsync() => Task.FromException<List<Libro>>(Missing());
    public Task<List<Libro>> BuscarLibrosAsync(string texto) => Task.FromException<List<Libro>>(Missing());
    public Task<int> CrearLibroAsync(Libro libro) => Task.FromException<int>(Missing());
    public Task ActualizarLibroAsync(Libro libro) => Task.FromException(Missing());
    public Task DarDeBajaLibroAsync(int id) => Task.FromException(Missing());
    public Task<List<Socio>> ListarSociosAsync() => Task.FromException<List<Socio>>(Missing());
    public Task<List<Socio>> BuscarSociosAsync(string texto) => Task.FromException<List<Socio>>(Missing());
    public Task<int> CrearSocioAsync(Socio socio) => Task.FromException<int>(Missing());
    public Task ActualizarSocioAsync(Socio socio) => Task.FromException(Missing());
    public Task DarDeBajaSocioAsync(int id) => Task.FromException(Missing());
    public Task<int> RegistrarPrestamoAsync(int socioId, List<int> libroIds) => Task.FromException<int>(Missing());
    public Task<Prestamo?> ObtenerPrestamoAsync(int id) => Task.FromException<Prestamo?>(Missing());
    public Task<decimal> RegistrarDevolucionAsync(int prestamoId, int libroId) => Task.FromException<decimal>(Missing());
    public Task<List<Prestamo>> ReporteAsync(DateTime desde, DateTime hasta) => Task.FromException<List<Prestamo>>(Missing());
}
