using System.Collections.ObjectModel;
using System.Globalization;
using Biblioteca.Entidades;
using Lab07.Commands;
using Lab07.Services;

namespace Lab07.ViewModels;

public sealed class ReturnsViewModel : PageViewModel
{
    private string _query = string.Empty;
    private Prestamo? _selectedLoan;
    private ReturnRow? _selectedDetail;
    private string _lastFine = string.Empty;
    public string Query { get => _query; set => Set(ref _query, value); }
    public ObservableCollection<Prestamo> Matches { get; } = new();
    public Prestamo? SelectedLoan { get => _selectedLoan; set { if (Set(ref _selectedLoan, value)) FillDetails(); } }
    public ObservableCollection<ReturnRow> Details { get; } = new();
    public ReturnRow? SelectedDetail { get => _selectedDetail; set => Set(ref _selectedDetail, value); }
    public string LastFine { get => _lastFine; private set => Set(ref _lastFine, value); }
    public AsyncCommand SearchCommand { get; }
    public Func<Task>? DataChanged { get; set; }
    public AsyncCommand ReturnCommand { get; }

    public ReturnsViewModel(IBibliotecaService service) : base(service)
    {
        SearchCommand = new(SearchAsync);
        ReturnCommand = new(ReturnAsync);
    }

    public override Task LoadAsync() => Task.CompletedTask;

    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(Query)) { Error = "Escribe un socio, DNI o número de préstamo."; return; }
        await RunAsync(async () =>
        {
            var search = Query.Trim();
            var members = await Service.BuscarSociosAsync(search);
            var ids = members.Select(x => x.SocioId).ToHashSet();
            var all = await Service.ReporteAsync(new DateTime(1753, 1, 1), DateTime.Today);
            var loans = all.Where(x => ids.Contains(x.SocioId) ||
                (x.SocioNombre?.Contains(search, StringComparison.CurrentCultureIgnoreCase) ?? false)).ToList();
            if (int.TryParse(search, out var id))
            {
                var byId = await Service.ObtenerPrestamoAsync(id);
                if (byId is not null && loans.All(x => x.PrestamoId != id)) loans.Add(byId);
            }
            Matches.Clear();
            foreach (var loan in loans.Where(x => x.Detalles.Any(d => d.FechaDevolucion is null)).OrderBy(x => x.FechaLimite)) Matches.Add(loan);
            SelectedLoan = Matches.FirstOrDefault();
            LastFine = string.Empty;
        });
    }

    private void FillDetails()
    {
        Details.Clear();
        SelectedDetail = null;
        if (SelectedLoan is null) return;
        foreach (var detail in SelectedLoan.Detalles)
            Details.Add(new ReturnRow(detail, SelectedLoan.FechaLimite));
    }

    private async Task ReturnAsync()
    {
        var row = SelectedDetail is { IsReturned: false } ? SelectedDetail : null;
        if (SelectedLoan is null || row is null) { Error = "Selecciona un libro pendiente para registrar su devolución."; return; }
        var loanId = SelectedLoan.PrestamoId;
        await RunAsync(async () =>
        {
            var fine = await Service.RegistrarDevolucionAsync(loanId, row.BookId);
            LastFine = $"Multa calculada por Negocio: S/ {fine.ToString("0.00", CultureInfo.InvariantCulture)}";
            Notice = "Devolución registrada correctamente.";
            var updated = await Service.ObtenerPrestamoAsync(loanId);
            if (updated is not null)
            {
                var index = Matches.IndexOf(SelectedLoan);
                if (index >= 0) Matches[index] = updated;
                SelectedLoan = updated;
            }
            if (DataChanged is not null) await DataChanged();
        });
    }
}

public sealed class ReturnRow : ObservableObject
{
    public ReturnRow(DetallePrestamo detail, DateTime due)
    {
        BookId = detail.LibroId;
        Title = detail.LibroTitulo ?? "—";
        Due = due;
        IsReturned = detail.FechaDevolucion is not null;
    }
    public int BookId { get; }
    public string Title { get; }
    public DateTime Due { get; }
    public string DueText => Due.ToString("dd MMM yyyy");
    public string DelayText => IsReturned ? "—" : $"{Math.Max(0, (DateTime.Today - Due.Date).Days)} día(s)";
    public bool IsReturned { get; }
    public string Status => IsReturned ? "Devuelto" : Due.Date < DateTime.Today ? "Pendiente · vencido" : "Pendiente";
    public string FineText => IsReturned ? "—" : "Se calcula al devolver";
}
