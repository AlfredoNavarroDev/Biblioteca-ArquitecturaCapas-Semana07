using System.Configuration;

namespace Biblioteca.Datos;

internal static class Conexion
{
    public static string CadenaConexion =>
        ConfigurationManager.ConnectionStrings["BibliotecaDB"]?.ConnectionString
        ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'BibliotecaDB' en el archivo de configuración.");
}
