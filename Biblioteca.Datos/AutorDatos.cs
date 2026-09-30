using Biblioteca.Entidades;
using Microsoft.Data.SqlClient;

namespace Biblioteca.Datos;

public class AutorDatos
{
    public async Task<List<Autor>> ListarActivosAsync()
    {
        var autores = new List<Autor>();

        using var conexion = new SqlConnection(Conexion.CadenaConexion);
        using var comando = new SqlCommand(
            "SELECT AutorId, Nombre, Nacionalidad, Activo FROM dbo.Autores WHERE Activo = 1 ORDER BY Nombre",
            conexion);

        await conexion.OpenAsync();
        using var lector = await comando.ExecuteReaderAsync();
        while (await lector.ReadAsync())
        {
            autores.Add(Mapear(lector));
        }

        return autores;
    }

    public async Task<Autor?> ObtenerPorIdAsync(int autorId)
    {
        using var conexion = new SqlConnection(Conexion.CadenaConexion);
        using var comando = new SqlCommand(
            "SELECT AutorId, Nombre, Nacionalidad, Activo FROM dbo.Autores WHERE AutorId = @AutorId",
            conexion);
        comando.Parameters.AddWithValue("@AutorId", autorId);

        await conexion.OpenAsync();
        using var lector = await comando.ExecuteReaderAsync();
        return await lector.ReadAsync() ? Mapear(lector) : null;
    }

    private static Autor Mapear(SqlDataReader lector) => new()
    {
        AutorId = lector.GetInt32(lector.GetOrdinal("AutorId")),
        Nombre = lector.GetString(lector.GetOrdinal("Nombre")),
        Nacionalidad = lector.GetString(lector.GetOrdinal("Nacionalidad")),
        Activo = lector.GetBoolean(lector.GetOrdinal("Activo"))
    };
}
