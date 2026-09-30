namespace Biblioteca.Entidades;

public class Autor
{
    public int AutorId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Nacionalidad { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
}
