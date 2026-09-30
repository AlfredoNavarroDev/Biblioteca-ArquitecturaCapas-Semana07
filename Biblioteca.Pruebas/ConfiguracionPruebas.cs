using System.Runtime.CompilerServices;

namespace Biblioteca.Pruebas;

internal static class ConfiguracionPruebas
{
    [ModuleInitializer]
    public static void FijarCadenaConexion()
    {
        Environment.SetEnvironmentVariable(
            "BIBLIOTECA_CONNECTION_STRING",
            @"Server=(localdb)\MSSQLLocalDB;Database=BibliotecaDB;Trusted_Connection=True;TrustServerCertificate=True;");
    }
}
