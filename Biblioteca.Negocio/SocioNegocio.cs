using Biblioteca.Datos;
using Biblioteca.Entidades;

namespace Biblioteca.Negocio;

public class SocioNegocio
{
    private readonly SocioDatos _socioDatos;

    public SocioNegocio(SocioDatos socioDatos)
    {
        _socioDatos = socioDatos;
    }

    public Task<List<Socio>> ListarAsync() => _socioDatos.ListarAsync();

    public Task<List<Socio>> BuscarPorNombreODniAsync(string texto) =>
        _socioDatos.BuscarPorNombreODniAsync(texto);

    public async Task<int> InsertarAsync(Socio socio)
    {
        ValidarDatosBasicos(socio);

        var existente = await _socioDatos.ObtenerPorDNIAsync(socio.DNI);
        if (existente is not null)
        {
            throw new ReglaNegocioException($"Ya existe un socio registrado con el DNI '{socio.DNI}'.");
        }

        return await _socioDatos.InsertarAsync(socio);
    }

    public async Task ActualizarAsync(Socio socio)
    {
        var actual = await _socioDatos.ObtenerPorIdAsync(socio.SocioId)
            ?? throw new ReglaNegocioException($"No existe el socio con id {socio.SocioId}.");

        ValidarDatosBasicos(socio);

        var conMismoDni = await _socioDatos.ObtenerPorDNIAsync(socio.DNI);
        if (conMismoDni is not null && conMismoDni.SocioId != socio.SocioId)
        {
            throw new ReglaNegocioException($"Ya existe otro socio registrado con el DNI '{socio.DNI}'.");
        }

        socio.Activo = actual.Activo;
        await _socioDatos.ActualizarAsync(socio);
    }

    public async Task DarDeBajaAsync(int socioId)
    {
        var socio = await _socioDatos.ObtenerPorIdAsync(socioId)
            ?? throw new ReglaNegocioException($"No existe el socio con id {socioId}.");

        var pendientes = await _socioDatos.ContarPrestamosPendientesAsync(socioId);
        if (pendientes > 0)
        {
            throw new ReglaNegocioException(
                $"No se puede dar de baja al socio '{socio.Nombre}' porque tiene {pendientes} libro(s) pendiente(s) de devolución.");
        }

        await _socioDatos.DarDeBajaAsync(socioId);
    }

    private static void ValidarDatosBasicos(Socio socio)
    {
        if (string.IsNullOrWhiteSpace(socio.DNI))
        {
            throw new ReglaNegocioException("El DNI del socio es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(socio.Nombre))
        {
            throw new ReglaNegocioException("El nombre del socio es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(socio.Email))
        {
            throw new ReglaNegocioException("El email del socio es obligatorio.");
        }
    }
}
