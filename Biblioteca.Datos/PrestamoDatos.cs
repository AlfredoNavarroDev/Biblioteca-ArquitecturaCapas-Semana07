using Biblioteca.Entidades;
using Microsoft.Data.SqlClient;

namespace Biblioteca.Datos;

public class PrestamoDatos
{
    public async Task<int> RegistrarAsync(int socioId, DateTime fechaPrestamo, DateTime fechaLimite, List<int> libroIds)
    {
        using var conexion = new SqlConnection(Conexion.CadenaConexion);
        await conexion.OpenAsync();
        using var transaccion = await conexion.BeginTransactionAsync();

        try
        {
            int prestamoId;
            using (var comandoCabecera = new SqlCommand(
                "INSERT INTO dbo.Prestamos (SocioId, FechaPrestamo, FechaLimite, Estado) " +
                "OUTPUT INSERTED.PrestamoId " +
                "VALUES (@SocioId, @FechaPrestamo, @FechaLimite, N'Pendiente')",
                conexion, (SqlTransaction)transaccion))
            {
                comandoCabecera.Parameters.AddWithValue("@SocioId", socioId);
                comandoCabecera.Parameters.AddWithValue("@FechaPrestamo", fechaPrestamo);
                comandoCabecera.Parameters.AddWithValue("@FechaLimite", fechaLimite);
                prestamoId = (int)(await comandoCabecera.ExecuteScalarAsync())!;
            }

            foreach (var libroId in libroIds)
            {
                using var comandoDetalle = new SqlCommand(
                    "INSERT INTO dbo.DetallePrestamo (PrestamoId, LibroId, FechaDevolucion) " +
                    "VALUES (@PrestamoId, @LibroId, NULL)",
                    conexion, (SqlTransaction)transaccion);
                comandoDetalle.Parameters.AddWithValue("@PrestamoId", prestamoId);
                comandoDetalle.Parameters.AddWithValue("@LibroId", libroId);
                await comandoDetalle.ExecuteNonQueryAsync();

                using var comandoStock = new SqlCommand(
                    "UPDATE dbo.Libros SET Ejemplares = Ejemplares - 1 WHERE LibroId = @LibroId",
                    conexion, (SqlTransaction)transaccion);
                comandoStock.Parameters.AddWithValue("@LibroId", libroId);
                await comandoStock.ExecuteNonQueryAsync();
            }

            await transaccion.CommitAsync();
            return prestamoId;
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    public async Task<decimal?> RegistrarDevolucionAsync(int prestamoId, int libroId, DateTime fechaDevolucion, decimal montoMultaPorDia)
    {
        using var conexion = new SqlConnection(Conexion.CadenaConexion);
        await conexion.OpenAsync();
        using var transaccion = await conexion.BeginTransactionAsync();

        try
        {
            DateTime fechaLimite;
            using (var comandoLimite = new SqlCommand(
                "SELECT FechaLimite FROM dbo.Prestamos WHERE PrestamoId = @PrestamoId",
                conexion, (SqlTransaction)transaccion))
            {
                comandoLimite.Parameters.AddWithValue("@PrestamoId", prestamoId);
                fechaLimite = (DateTime)(await comandoLimite.ExecuteScalarAsync())!;
            }

            using (var comandoDevolucion = new SqlCommand(
                "UPDATE dbo.DetallePrestamo SET FechaDevolucion = @Fecha " +
                "WHERE PrestamoId = @PrestamoId AND LibroId = @LibroId AND FechaDevolucion IS NULL",
                conexion, (SqlTransaction)transaccion))
            {
                comandoDevolucion.Parameters.AddWithValue("@Fecha", fechaDevolucion);
                comandoDevolucion.Parameters.AddWithValue("@PrestamoId", prestamoId);
                comandoDevolucion.Parameters.AddWithValue("@LibroId", libroId);
                await comandoDevolucion.ExecuteNonQueryAsync();
            }

            using (var comandoStock = new SqlCommand(
                "UPDATE dbo.Libros SET Ejemplares = Ejemplares + 1 WHERE LibroId = @LibroId",
                conexion, (SqlTransaction)transaccion))
            {
                comandoStock.Parameters.AddWithValue("@LibroId", libroId);
                await comandoStock.ExecuteNonQueryAsync();
            }

            int pendientes;
            using (var comandoPendientes = new SqlCommand(
                "SELECT COUNT(*) FROM dbo.DetallePrestamo WHERE PrestamoId = @PrestamoId AND FechaDevolucion IS NULL",
                conexion, (SqlTransaction)transaccion))
            {
                comandoPendientes.Parameters.AddWithValue("@PrestamoId", prestamoId);
                pendientes = (int)(await comandoPendientes.ExecuteScalarAsync())!;
            }

            if (pendientes == 0)
            {
                using var comandoEstado = new SqlCommand(
                    "UPDATE dbo.Prestamos SET Estado = N'Devuelto' WHERE PrestamoId = @PrestamoId",
                    conexion, (SqlTransaction)transaccion);
                comandoEstado.Parameters.AddWithValue("@PrestamoId", prestamoId);
                await comandoEstado.ExecuteNonQueryAsync();
            }

            await transaccion.CommitAsync();

            var diasAtraso = (fechaDevolucion.Date - fechaLimite.Date).Days;
            return diasAtraso > 0 ? diasAtraso * montoMultaPorDia : 0m;
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    public async Task<Prestamo?> ObtenerPorIdAsync(int prestamoId)
    {
        using var conexion = new SqlConnection(Conexion.CadenaConexion);
        await conexion.OpenAsync();

        Prestamo? prestamo = null;
        using (var comandoCabecera = new SqlCommand(
            "SELECT p.PrestamoId, p.SocioId, s.Nombre AS SocioNombre, p.FechaPrestamo, p.FechaLimite, p.Estado " +
            "FROM dbo.Prestamos p INNER JOIN dbo.Socios s ON p.SocioId = s.SocioId " +
            "WHERE p.PrestamoId = @PrestamoId",
            conexion))
        {
            comandoCabecera.Parameters.AddWithValue("@PrestamoId", prestamoId);
            using var lector = await comandoCabecera.ExecuteReaderAsync();
            if (await lector.ReadAsync())
            {
                prestamo = new Prestamo
                {
                    PrestamoId = lector.GetInt32(lector.GetOrdinal("PrestamoId")),
                    SocioId = lector.GetInt32(lector.GetOrdinal("SocioId")),
                    SocioNombre = lector.GetString(lector.GetOrdinal("SocioNombre")),
                    FechaPrestamo = lector.GetDateTime(lector.GetOrdinal("FechaPrestamo")),
                    FechaLimite = lector.GetDateTime(lector.GetOrdinal("FechaLimite")),
                    Estado = lector.GetString(lector.GetOrdinal("Estado"))
                };
            }
        }

        if (prestamo is null)
        {
            return null;
        }

        using (var comandoDetalle = new SqlCommand(
            "SELECT dp.PrestamoId, dp.LibroId, l.Titulo AS LibroTitulo, dp.FechaDevolucion " +
            "FROM dbo.DetallePrestamo dp INNER JOIN dbo.Libros l ON dp.LibroId = l.LibroId " +
            "WHERE dp.PrestamoId = @PrestamoId",
            conexion))
        {
            comandoDetalle.Parameters.AddWithValue("@PrestamoId", prestamoId);
            using var lector = await comandoDetalle.ExecuteReaderAsync();
            while (await lector.ReadAsync())
            {
                prestamo.Detalles.Add(new DetallePrestamo
                {
                    PrestamoId = lector.GetInt32(lector.GetOrdinal("PrestamoId")),
                    LibroId = lector.GetInt32(lector.GetOrdinal("LibroId")),
                    LibroTitulo = lector.GetString(lector.GetOrdinal("LibroTitulo")),
                    FechaDevolucion = lector.IsDBNull(lector.GetOrdinal("FechaDevolucion"))
                        ? null
                        : lector.GetDateTime(lector.GetOrdinal("FechaDevolucion"))
                });
            }
        }

        return prestamo;
    }

    public async Task<List<Prestamo>> ReportePorRangoFechasAsync(DateTime desde, DateTime hasta)
    {
        var prestamos = new Dictionary<int, Prestamo>();

        using var conexion = new SqlConnection(Conexion.CadenaConexion);
        using var comando = new SqlCommand(
            "SELECT p.PrestamoId, p.SocioId, s.Nombre AS SocioNombre, p.FechaPrestamo, p.FechaLimite, p.Estado, " +
            "dp.LibroId, l.Titulo AS LibroTitulo, dp.FechaDevolucion " +
            "FROM dbo.Prestamos p " +
            "INNER JOIN dbo.Socios s ON p.SocioId = s.SocioId " +
            "INNER JOIN dbo.DetallePrestamo dp ON p.PrestamoId = dp.PrestamoId " +
            "INNER JOIN dbo.Libros l ON dp.LibroId = l.LibroId " +
            "WHERE p.FechaPrestamo BETWEEN @Desde AND @Hasta " +
            "ORDER BY p.FechaPrestamo",
            conexion);
        comando.Parameters.AddWithValue("@Desde", desde.Date);
        comando.Parameters.AddWithValue("@Hasta", hasta.Date.AddDays(1).AddTicks(-1));

        await conexion.OpenAsync();
        using var lector = await comando.ExecuteReaderAsync();
        while (await lector.ReadAsync())
        {
            var prestamoId = lector.GetInt32(lector.GetOrdinal("PrestamoId"));
            if (!prestamos.TryGetValue(prestamoId, out var prestamo))
            {
                prestamo = new Prestamo
                {
                    PrestamoId = prestamoId,
                    SocioId = lector.GetInt32(lector.GetOrdinal("SocioId")),
                    SocioNombre = lector.GetString(lector.GetOrdinal("SocioNombre")),
                    FechaPrestamo = lector.GetDateTime(lector.GetOrdinal("FechaPrestamo")),
                    FechaLimite = lector.GetDateTime(lector.GetOrdinal("FechaLimite")),
                    Estado = lector.GetString(lector.GetOrdinal("Estado"))
                };
                prestamos[prestamoId] = prestamo;
            }

            prestamo.Detalles.Add(new DetallePrestamo
            {
                PrestamoId = prestamoId,
                LibroId = lector.GetInt32(lector.GetOrdinal("LibroId")),
                LibroTitulo = lector.GetString(lector.GetOrdinal("LibroTitulo")),
                FechaDevolucion = lector.IsDBNull(lector.GetOrdinal("FechaDevolucion"))
                    ? null
                    : lector.GetDateTime(lector.GetOrdinal("FechaDevolucion"))
            });
        }

        return prestamos.Values.ToList();
    }
}
