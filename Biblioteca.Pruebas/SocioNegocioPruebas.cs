using Biblioteca.Datos;
using Biblioteca.Entidades;
using Biblioteca.Negocio;
using Xunit;

namespace Biblioteca.Pruebas;

public class SocioNegocioPruebas
{
    private static SocioNegocio CrearSocioNegocio() => new(new SocioDatos());

    [Fact]
    public async Task InsertarAsync_ConDniDuplicado_LanzaReglaNegocioException()
    {
        var negocio = CrearSocioNegocio();
        var socio = new Socio
        {
            DNI = "71234561", // DNI de Ana Torres, ya sembrado
            Nombre = "Duplicado de prueba",
            Email = "duplicado@correo.com"
        };

        await Assert.ThrowsAsync<ReglaNegocioException>(() => negocio.InsertarAsync(socio));
    }

    [Fact]
    public async Task DarDeBajaAsync_ConPrestamosPendientes_LanzaReglaNegocioException()
    {
        var negocio = CrearSocioNegocio();

        // SocioId 1 (Ana Torres) tiene 3 libros pendientes de devolución.
        await Assert.ThrowsAsync<ReglaNegocioException>(() => negocio.DarDeBajaAsync(1));
    }

    [Fact]
    public async Task InsertarAsync_ConDatosValidos_RetornaIdGenerado()
    {
        var negocio = CrearSocioNegocio();
        var dniUnico = Guid.NewGuid().GetHashCode().ToString("D8")[..8];
        var socio = new Socio
        {
            DNI = dniUnico,
            Nombre = "Socio de prueba",
            Email = "socio.prueba@correo.com"
        };

        var socioId = await negocio.InsertarAsync(socio);

        Assert.True(socioId > 0);
    }
}
