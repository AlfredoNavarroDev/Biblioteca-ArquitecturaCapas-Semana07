namespace Biblioteca.Entidades;

public class Libro
{
    public int LibroId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string ISBN { get; set; } = string.Empty;
    public int AutorId { get; set; }
    public string? AutorNombre { get; set; }
    public int Ejemplares { get; set; }
    public bool Activo { get; set; } = true;
}
