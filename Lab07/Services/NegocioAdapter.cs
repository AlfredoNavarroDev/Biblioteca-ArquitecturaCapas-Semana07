using Biblioteca.Entidades;
using Biblioteca.Negocio;

namespace Lab07.Services;

// A composition root outside WPF must supply the three existing business objects.
public sealed class NegocioAdapter : IBibliotecaService
{
    private readonly LibroNegocio _libros;
    private readonly SocioNegocio _socios;
    private readonly PrestamoNegocio _prestamos;

    public NegocioAdapter(LibroNegocio libros, SocioNegocio socios, PrestamoNegocio prestamos)
    {
        _libros = libros;
        _socios = socios;
        _prestamos = prestamos;
    }

    public Task<List<Libro>> ListarLibrosAsync() => _libros.ListarAsync();
    public Task<List<Libro>> BuscarLibrosAsync(string texto) => _libros.BuscarPorTituloOAutorAsync(texto);
    public Task<int> CrearLibroAsync(Libro libro) => _libros.InsertarAsync(libro);
    public Task ActualizarLibroAsync(Libro libro) => _libros.ActualizarAsync(libro);
    public Task DarDeBajaLibroAsync(int id) => _libros.DarDeBajaAsync(id);
    public Task<List<Socio>> ListarSociosAsync() => _socios.ListarAsync();
    public Task<List<Socio>> BuscarSociosAsync(string texto) => _socios.BuscarPorNombreODniAsync(texto);
    public Task<int> CrearSocioAsync(Socio socio) => _socios.InsertarAsync(socio);
    public Task ActualizarSocioAsync(Socio socio) => _socios.ActualizarAsync(socio);
    public Task DarDeBajaSocioAsync(int id) => _socios.DarDeBajaAsync(id);
    public Task<int> RegistrarPrestamoAsync(int socioId, List<int> libroIds) => _prestamos.RegistrarPrestamoAsync(socioId, libroIds);
    public Task<Prestamo?> ObtenerPrestamoAsync(int id) => _prestamos.ObtenerPorIdAsync(id);
    public Task<decimal> RegistrarDevolucionAsync(int prestamoId, int libroId) => _prestamos.RegistrarDevolucionAsync(prestamoId, libroId);
    public Task<List<Prestamo>> ReporteAsync(DateTime desde, DateTime hasta) => _prestamos.ReportePorRangoFechasAsync(desde, hasta);
}
