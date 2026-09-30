using Biblioteca.Entidades;
using Microsoft.Data.SqlClient;

namespace Biblioteca.Datos;

public class LibroDatos : ILibroRepositorio
{
    private const string SeleccionBase =
        "SELECT l.LibroId, l.Titulo, l.ISBN, l.AutorId, a.Nombre AS AutorNombre, l.Ejemplares, l.Activo " +
        "FROM dbo.Libros l INNER JOIN dbo.Autores a ON l.AutorId = a.AutorId";

    public async Task<List<Libro>> ListarAsync()
    {
        var libros = new List<Libro>();

        using var conexion = new SqlConnection(Conexion.CadenaConexion);
        using var comando = new SqlCommand(SeleccionBase + " WHERE l.Activo = 1 ORDER BY l.Titulo", conexion);

        await conexion.OpenAsync();
        using var lector = await comando.ExecuteReaderAsync();
        while (await lector.ReadAsync())
        {
            libros.Add(Mapear(lector));
        }

        return libros;
    }

    public async Task<Libro?> ObtenerPorIdAsync(int libroId)
    {
        using var conexion = new SqlConnection(Conexion.CadenaConexion);
        using var comando = new SqlCommand(SeleccionBase + " WHERE l.LibroId = @LibroId", conexion);
        comando.Parameters.AddWithValue("@LibroId", libroId);

        await conexion.OpenAsync();
        using var lector = await comando.ExecuteReaderAsync();
        return await lector.ReadAsync() ? Mapear(lector) : null;
    }

    public async Task<Libro?> ObtenerPorISBNAsync(string isbn)
    {
        using var conexion = new SqlConnection(Conexion.CadenaConexion);
        using var comando = new SqlCommand(SeleccionBase + " WHERE l.ISBN = @ISBN", conexion);
        comando.Parameters.AddWithValue("@ISBN", isbn);

        await conexion.OpenAsync();
        using var lector = await comando.ExecuteReaderAsync();
        return await lector.ReadAsync() ? Mapear(lector) : null;
    }

    public async Task<List<Libro>> BuscarPorTituloOAutorAsync(string texto)
    {
        var libros = new List<Libro>();

        using var conexion = new SqlConnection(Conexion.CadenaConexion);
        using var comando = new SqlCommand(
            SeleccionBase + " WHERE l.Activo = 1 AND (l.Titulo LIKE @Texto OR a.Nombre LIKE @Texto) ORDER BY l.Titulo",
            conexion);
        comando.Parameters.AddWithValue("@Texto", $"%{texto}%");

        await conexion.OpenAsync();
        using var lector = await comando.ExecuteReaderAsync();
        while (await lector.ReadAsync())
        {
            libros.Add(Mapear(lector));
        }

        return libros;
    }

    public async Task<int> InsertarAsync(Libro libro)
    {
        using var conexion = new SqlConnection(Conexion.CadenaConexion);
        using var comando = new SqlCommand(
            "INSERT INTO dbo.Libros (Titulo, ISBN, AutorId, Ejemplares, Activo) " +
            "OUTPUT INSERTED.LibroId " +
            "VALUES (@Titulo, @ISBN, @AutorId, @Ejemplares, 1)",
            conexion);
        comando.Parameters.AddWithValue("@Titulo", libro.Titulo);
        comando.Parameters.AddWithValue("@ISBN", libro.ISBN);
        comando.Parameters.AddWithValue("@AutorId", libro.AutorId);
        comando.Parameters.AddWithValue("@Ejemplares", libro.Ejemplares);

        await conexion.OpenAsync();
        return (int)(await comando.ExecuteScalarAsync())!;
    }

    public async Task ActualizarAsync(Libro libro)
    {
        using var conexion = new SqlConnection(Conexion.CadenaConexion);
        using var comando = new SqlCommand(
            "UPDATE dbo.Libros SET Titulo = @Titulo, ISBN = @ISBN, AutorId = @AutorId, Ejemplares = @Ejemplares " +
            "WHERE LibroId = @LibroId",
            conexion);
        comando.Parameters.AddWithValue("@Titulo", libro.Titulo);
        comando.Parameters.AddWithValue("@ISBN", libro.ISBN);
        comando.Parameters.AddWithValue("@AutorId", libro.AutorId);
        comando.Parameters.AddWithValue("@Ejemplares", libro.Ejemplares);
        comando.Parameters.AddWithValue("@LibroId", libro.LibroId);

        await conexion.OpenAsync();
        await comando.ExecuteNonQueryAsync();
    }

    public async Task DarDeBajaAsync(int libroId)
    {
        using var conexion = new SqlConnection(Conexion.CadenaConexion);
        using var comando = new SqlCommand("UPDATE dbo.Libros SET Activo = 0 WHERE LibroId = @LibroId", conexion);
        comando.Parameters.AddWithValue("@LibroId", libroId);

        await conexion.OpenAsync();
        await comando.ExecuteNonQueryAsync();
    }

    public async Task<int> ContarPrestamosPendientesAsync(int libroId)
    {
        using var conexion = new SqlConnection(Conexion.CadenaConexion);
        using var comando = new SqlCommand(
            "SELECT COUNT(*) FROM dbo.DetallePrestamo WHERE LibroId = @LibroId AND FechaDevolucion IS NULL",
            conexion);
        comando.Parameters.AddWithValue("@LibroId", libroId);

        await conexion.OpenAsync();
        return (int)(await comando.ExecuteScalarAsync())!;
    }

    private static Libro Mapear(SqlDataReader lector) => new()
    {
        LibroId = lector.GetInt32(lector.GetOrdinal("LibroId")),
        Titulo = lector.GetString(lector.GetOrdinal("Titulo")),
        ISBN = lector.GetString(lector.GetOrdinal("ISBN")),
        AutorId = lector.GetInt32(lector.GetOrdinal("AutorId")),
        AutorNombre = lector.GetString(lector.GetOrdinal("AutorNombre")),
        Ejemplares = lector.GetInt32(lector.GetOrdinal("Ejemplares")),
        Activo = lector.GetBoolean(lector.GetOrdinal("Activo"))
    };
}
