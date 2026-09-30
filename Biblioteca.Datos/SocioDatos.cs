using Biblioteca.Entidades;
using Microsoft.Data.SqlClient;

namespace Biblioteca.Datos;

public class SocioDatos
{
    private const string SeleccionBase = "SELECT SocioId, DNI, Nombre, Email, Activo FROM dbo.Socios";

    public async Task<List<Socio>> ListarAsync()
    {
        var socios = new List<Socio>();

        using var conexion = new SqlConnection(Conexion.CadenaConexion);
        using var comando = new SqlCommand(SeleccionBase + " WHERE Activo = 1 ORDER BY Nombre", conexion);

        await conexion.OpenAsync();
        using var lector = await comando.ExecuteReaderAsync();
        while (await lector.ReadAsync())
        {
            socios.Add(Mapear(lector));
        }

        return socios;
    }

    public async Task<Socio?> ObtenerPorIdAsync(int socioId)
    {
        using var conexion = new SqlConnection(Conexion.CadenaConexion);
        using var comando = new SqlCommand(SeleccionBase + " WHERE SocioId = @SocioId", conexion);
        comando.Parameters.AddWithValue("@SocioId", socioId);

        await conexion.OpenAsync();
        using var lector = await comando.ExecuteReaderAsync();
        return await lector.ReadAsync() ? Mapear(lector) : null;
    }

    public async Task<Socio?> ObtenerPorDNIAsync(string dni)
    {
        using var conexion = new SqlConnection(Conexion.CadenaConexion);
        using var comando = new SqlCommand(SeleccionBase + " WHERE DNI = @DNI", conexion);
        comando.Parameters.AddWithValue("@DNI", dni);

        await conexion.OpenAsync();
        using var lector = await comando.ExecuteReaderAsync();
        return await lector.ReadAsync() ? Mapear(lector) : null;
    }

    public async Task<List<Socio>> BuscarPorNombreODniAsync(string texto)
    {
        var socios = new List<Socio>();

        using var conexion = new SqlConnection(Conexion.CadenaConexion);
        using var comando = new SqlCommand(
            SeleccionBase + " WHERE Activo = 1 AND (Nombre LIKE @Texto OR DNI LIKE @Texto) ORDER BY Nombre",
            conexion);
        comando.Parameters.AddWithValue("@Texto", $"%{texto}%");

        await conexion.OpenAsync();
        using var lector = await comando.ExecuteReaderAsync();
        while (await lector.ReadAsync())
        {
            socios.Add(Mapear(lector));
        }

        return socios;
    }

    public async Task<int> InsertarAsync(Socio socio)
    {
        using var conexion = new SqlConnection(Conexion.CadenaConexion);
        using var comando = new SqlCommand(
            "INSERT INTO dbo.Socios (DNI, Nombre, Email, Activo) " +
            "OUTPUT INSERTED.SocioId " +
            "VALUES (@DNI, @Nombre, @Email, 1)",
            conexion);
        comando.Parameters.AddWithValue("@DNI", socio.DNI);
        comando.Parameters.AddWithValue("@Nombre", socio.Nombre);
        comando.Parameters.AddWithValue("@Email", socio.Email);

        await conexion.OpenAsync();
        return (int)(await comando.ExecuteScalarAsync())!;
    }

    public async Task ActualizarAsync(Socio socio)
    {
        using var conexion = new SqlConnection(Conexion.CadenaConexion);
        using var comando = new SqlCommand(
            "UPDATE dbo.Socios SET DNI = @DNI, Nombre = @Nombre, Email = @Email WHERE SocioId = @SocioId",
            conexion);
        comando.Parameters.AddWithValue("@DNI", socio.DNI);
        comando.Parameters.AddWithValue("@Nombre", socio.Nombre);
        comando.Parameters.AddWithValue("@Email", socio.Email);
        comando.Parameters.AddWithValue("@SocioId", socio.SocioId);

        await conexion.OpenAsync();
        await comando.ExecuteNonQueryAsync();
    }

    public async Task DarDeBajaAsync(int socioId)
    {
        using var conexion = new SqlConnection(Conexion.CadenaConexion);
        using var comando = new SqlCommand("UPDATE dbo.Socios SET Activo = 0 WHERE SocioId = @SocioId", conexion);
        comando.Parameters.AddWithValue("@SocioId", socioId);

        await conexion.OpenAsync();
        await comando.ExecuteNonQueryAsync();
    }

    public async Task<int> ContarPrestamosPendientesAsync(int socioId)
    {
        using var conexion = new SqlConnection(Conexion.CadenaConexion);
        using var comando = new SqlCommand(
            "SELECT COUNT(*) FROM dbo.DetallePrestamo dp " +
            "INNER JOIN dbo.Prestamos p ON dp.PrestamoId = p.PrestamoId " +
            "WHERE p.SocioId = @SocioId AND dp.FechaDevolucion IS NULL",
            conexion);
        comando.Parameters.AddWithValue("@SocioId", socioId);

        await conexion.OpenAsync();
        return (int)(await comando.ExecuteScalarAsync())!;
    }

    private static Socio Mapear(SqlDataReader lector) => new()
    {
        SocioId = lector.GetInt32(lector.GetOrdinal("SocioId")),
        DNI = lector.GetString(lector.GetOrdinal("DNI")),
        Nombre = lector.GetString(lector.GetOrdinal("Nombre")),
        Email = lector.GetString(lector.GetOrdinal("Email")),
        Activo = lector.GetBoolean(lector.GetOrdinal("Activo"))
    };
}
