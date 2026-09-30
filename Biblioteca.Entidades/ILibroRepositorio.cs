namespace Biblioteca.Entidades;

public interface ILibroRepositorio
{
    Task<List<Libro>> ListarAsync();
    Task<Libro?> ObtenerPorIdAsync(int libroId);
    Task<Libro?> ObtenerPorISBNAsync(string isbn);
    Task<List<Libro>> BuscarPorTituloOAutorAsync(string texto);
    Task<int> InsertarAsync(Libro libro);
    Task ActualizarAsync(Libro libro);
    Task DarDeBajaAsync(int libroId);
    Task<int> ContarPrestamosPendientesAsync(int libroId);
}
