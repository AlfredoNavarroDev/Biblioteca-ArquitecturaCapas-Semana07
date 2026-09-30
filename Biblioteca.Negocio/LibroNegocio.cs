using Biblioteca.Datos;
using Biblioteca.Entidades;

namespace Biblioteca.Negocio;

public class LibroNegocio
{
    private readonly ILibroRepositorio _libroRepositorio;
    private readonly AutorDatos _autorDatos;

    public LibroNegocio(ILibroRepositorio libroRepositorio, AutorDatos autorDatos)
    {
        _libroRepositorio = libroRepositorio;
        _autorDatos = autorDatos;
    }

    public Task<List<Libro>> ListarAsync() => _libroRepositorio.ListarAsync();

    public Task<List<Libro>> BuscarPorTituloOAutorAsync(string texto) =>
        _libroRepositorio.BuscarPorTituloOAutorAsync(texto);

    public async Task<int> InsertarAsync(Libro libro)
    {
        await ValidarDatosBasicosAsync(libro);

        var existente = await _libroRepositorio.ObtenerPorISBNAsync(libro.ISBN);
        if (existente is not null)
        {
            throw new ReglaNegocioException($"Ya existe un libro registrado con el ISBN '{libro.ISBN}'.");
        }

        return await _libroRepositorio.InsertarAsync(libro);
    }

    public async Task ActualizarAsync(Libro libro)
    {
        var actual = await _libroRepositorio.ObtenerPorIdAsync(libro.LibroId)
            ?? throw new ReglaNegocioException($"No existe el libro con id {libro.LibroId}.");

        await ValidarDatosBasicosAsync(libro);

        var conMismoIsbn = await _libroRepositorio.ObtenerPorISBNAsync(libro.ISBN);
        if (conMismoIsbn is not null && conMismoIsbn.LibroId != libro.LibroId)
        {
            throw new ReglaNegocioException($"Ya existe otro libro registrado con el ISBN '{libro.ISBN}'.");
        }

        libro.Activo = actual.Activo;
        await _libroRepositorio.ActualizarAsync(libro);
    }

    public async Task DarDeBajaAsync(int libroId)
    {
        var libro = await _libroRepositorio.ObtenerPorIdAsync(libroId)
            ?? throw new ReglaNegocioException($"No existe el libro con id {libroId}.");

        var pendientes = await _libroRepositorio.ContarPrestamosPendientesAsync(libroId);
        if (pendientes > 0)
        {
            throw new ReglaNegocioException(
                $"No se puede dar de baja el libro '{libro.Titulo}' porque tiene {pendientes} préstamo(s) pendiente(s).");
        }

        await _libroRepositorio.DarDeBajaAsync(libroId);
    }

    private async Task ValidarDatosBasicosAsync(Libro libro)
    {
        if (string.IsNullOrWhiteSpace(libro.Titulo))
        {
            throw new ReglaNegocioException("El título del libro es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(libro.ISBN))
        {
            throw new ReglaNegocioException("El ISBN del libro es obligatorio.");
        }

        if (libro.Ejemplares < 0)
        {
            throw new ReglaNegocioException("El número de ejemplares no puede ser negativo.");
        }

        var autor = await _autorDatos.ObtenerPorIdAsync(libro.AutorId);
        if (autor is null || !autor.Activo)
        {
            throw new ReglaNegocioException($"El autor con id {libro.AutorId} no existe o no está activo.");
        }
    }
}
