using Biblioteca.Datos;
using Biblioteca.Entidades;
using Biblioteca.Negocio;
using Xunit;

namespace Biblioteca.Pruebas;

/// <summary>
/// Estas pruebas asumen la base BibliotecaDB recién sembrada con Database/01_Schema.sql
/// y Database/02_Seed.sql (PrestamoId 1 = Ana Torres con 3 pendientes, PrestamoId 4 = Jorge Salas vencido).
/// </summary>
public class PrestamoNegocioPruebas
{
    private static PrestamoNegocio CrearPrestamoNegocio() =>
        new(new PrestamoDatos(), new SocioDatos(), new LibroDatos());

    private static async Task<int> CrearSocioDePruebaAsync()
    {
        var socioNegocio = new SocioNegocio(new SocioDatos());
        var dniUnico = Math.Abs(Guid.NewGuid().GetHashCode()).ToString()[..8];
        return await socioNegocio.InsertarAsync(new Socio
        {
            DNI = dniUnico,
            Nombre = "Socio de prueba préstamos",
            Email = "prestamo.prueba@correo.com"
        });
    }

    [Fact]
    public async Task RegistrarPrestamoAsync_SocioConTresPendientes_LanzaReglaNegocioException()
    {
        var negocio = CrearPrestamoNegocio();

        // SocioId 1 (Ana Torres) ya tiene 3 libros pendientes en el seed.
        await Assert.ThrowsAsync<ReglaNegocioException>(
            () => negocio.RegistrarPrestamoAsync(1, new List<int> { 2 }));
    }

    [Fact]
    public async Task RegistrarPrestamoAsync_LibroSinEjemplares_LanzaReglaNegocioException()
    {
        var negocio = CrearPrestamoNegocio();
        var socioId = await CrearSocioDePruebaAsync();

        // LibroId 13 ("Bestiario") tiene 0 ejemplares en el seed.
        await Assert.ThrowsAsync<ReglaNegocioException>(
            () => negocio.RegistrarPrestamoAsync(socioId, new List<int> { 13 }));
    }

    [Fact]
    public async Task RegistrarPrestamoAsync_ConDatosValidos_DescuentaEjemplarYCreaPrestamoPendiente()
    {
        var negocio = CrearPrestamoNegocio();
        var libroDatos = new LibroDatos();
        var socioId = await CrearSocioDePruebaAsync();

        const int libroId = 14; // "Veinte poemas de amor", 5 ejemplares en el seed
        var libroAntes = await libroDatos.ObtenerPorIdAsync(libroId);

        var prestamoId = await negocio.RegistrarPrestamoAsync(socioId, new List<int> { libroId });

        var libroDespues = await libroDatos.ObtenerPorIdAsync(libroId);
        var prestamo = await negocio.ObtenerPorIdAsync(prestamoId);

        Assert.Equal(libroAntes!.Ejemplares - 1, libroDespues!.Ejemplares);
        Assert.NotNull(prestamo);
        Assert.Equal("Pendiente", prestamo!.Estado);
        Assert.Single(prestamo.Detalles);
    }

    [Fact]
    public async Task RegistrarDevolucionAsync_ConAtraso_CalculaMultaYCierraPrestamo()
    {
        var negocio = CrearPrestamoNegocio();

        // PrestamoId 4 (Jorge Salas): FechaLimite 2026-09-03, un solo libro pendiente (LibroId 12).
        var fechaDevolucion = new DateTime(2026, 9, 10); // 7 días de atraso

        var multa = await negocio.RegistrarDevolucionAsync(4, 12, fechaDevolucion);
        var prestamo = await negocio.ObtenerPorIdAsync(4);

        Assert.Equal(10.50m, multa);
        Assert.Equal("Devuelto", prestamo!.Estado);
    }

    [Fact]
    public async Task PrestamoDatos_RegistrarAsync_ConLibroInexistente_NoGuardaNadaPorLaTransaccion()
    {
        var prestamoDatos = new PrestamoDatos();
        var socioDatos = new SocioDatos();
        var socioId = await CrearSocioDePruebaAsync();

        // LibroId 1 es válido, 999999 no existe -> debe violar la FK y revertir toda la transacción.
        await Assert.ThrowsAnyAsync<Exception>(() =>
            prestamoDatos.RegistrarAsync(socioId, DateTime.Now, DateTime.Now.AddDays(14), new List<int> { 1, 999999 }));

        var pendientes = await socioDatos.ContarPrestamosPendientesAsync(socioId);
        Assert.Equal(0, pendientes);
    }
}
