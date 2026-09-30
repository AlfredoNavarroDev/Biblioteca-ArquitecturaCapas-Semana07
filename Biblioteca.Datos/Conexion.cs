using System.Configuration;

namespace Biblioteca.Datos;

internal static class Conexion
{
    // La variable de entorno permite a los hosts de pruebas (que no exponen su propio
    // App.config a ConfigurationManager) inyectar la cadena de conexión. La aplicación
    // WPF nunca la define, así que en producción siempre se usa el App.config.
    public static string CadenaConexion =>
        Environment.GetEnvironmentVariable("BIBLIOTECA_CONNECTION_STRING")
        ?? ConfigurationManager.ConnectionStrings["BibliotecaDB"]?.ConnectionString
        ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'BibliotecaDB' en el archivo de configuración.");
}
