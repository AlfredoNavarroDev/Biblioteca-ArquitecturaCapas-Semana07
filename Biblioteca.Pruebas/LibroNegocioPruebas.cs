using Biblioteca.Datos;
using Biblioteca.Entidades;
using Biblioteca.Negocio;
using Xunit;

namespace Biblioteca.Pruebas;

public class LibroNegocioPruebas
{
    private static LibroNegocio CrearLibroNegocio() => new(new LibroDatos(), new AutorDatos());

    [Fact]
    public async Task InsertarAsync_ConIsbnDuplicado_LanzaReglaNegocioException()
    {
        var negocio = CrearLibroNegocio();
        var libro = new Libro
        {
            Titulo = "Libro de prueba duplicado",
            ISBN = "978-0307474728", // ISBN de 'Cien años de soledad', ya sembrado
            AutorId = 1,
            Ejemplares = 2
        };

        await Assert.ThrowsAsync<ReglaNegocioException>(() => negocio.InsertarAsync(libro));
    }

    [Fact]
    public async Task DarDeBajaAsync_ConPrestamosPendientes_LanzaReglaNegocioException()
    {
        var negocio = CrearLibroNegocio();

        // LibroId 1 ("Cien años de soledad") está en el Prestamo 1 (Ana Torres) sin devolver.
        await Assert.ThrowsAsync<ReglaNegocioException>(() => negocio.DarDeBajaAsync(1));
    }

    [Fact]
    public async Task InsertarAsync_ConDatosValidos_RetornaIdGenerado()
    {
        var negocio = CrearLibroNegocio();
        var isbnUnico = $"978-0-{Guid.NewGuid().GetHashCode():X8}";
        var libro = new Libro
        {
            Titulo = "Libro de prueba nuevo",
            ISBN = isbnUnico,
            AutorId = 1,
            Ejemplares = 5
        };

        var libroId = await negocio.InsertarAsync(libro);

        Assert.True(libroId > 0);
    }
}
