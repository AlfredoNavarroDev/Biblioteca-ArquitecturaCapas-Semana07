using System.Collections.ObjectModel;
using Biblioteca.Entidades;
using Lab07.Commands;
using Lab07.Services;

namespace Lab07.ViewModels;

public sealed class HomeViewModel : PageViewModel
{
    private string _available = "—";
    private string _pending = "—";
    private string _overdue = "—";
    public string Available { get => _available; private set => Set(ref _available, value); }
    public string Pending { get => _pending; private set => Set(ref _pending, value); }
    public string Overdue { get => _overdue; private set => Set(ref _overdue, value); }
    public ObservableCollection<UpcomingRow> Upcoming { get; } = new();
    public AsyncCommand RefreshCommand { get; }

    public HomeViewModel(IBibliotecaService service) : base(service) => RefreshCommand = new(LoadAsync);

    public override Task LoadAsync() => RunAsync(async () =>
    {
        var books = await Service.ListarLibrosAsync();
        var loans = await Service.ReporteAsync(new DateTime(1753, 1, 1), DateTime.Today);
        Available = books.Sum(x => x.Ejemplares).ToString("N0");
        var pending = loans.SelectMany(p => p.Detalles.Where(d => d.FechaDevolucion is null)
            .Select(d => new UpcomingRow(p.SocioNombre ?? "—", d.LibroTitulo ?? "—", p.FechaLimite, p.PrestamoId))).ToList();
        Pending = pending.Count.ToString("N0");
        Overdue = pending.Count(x => x.Due.Date < DateTime.Today).ToString("N0");
        Upcoming.Clear();
        foreach (var row in pending.OrderBy(x => x.Due).Take(8)) Upcoming.Add(row);
    });
}

public sealed record UpcomingRow(string Member, string Book, DateTime Due, int LoanId)
{
    public string DueText => Due.ToString("dd MMM yyyy");
    public string Condition => Due.Date < DateTime.Today ? "Vencido" : "Próximo";
}
