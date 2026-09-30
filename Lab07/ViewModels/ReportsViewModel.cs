using System.Collections.ObjectModel;
using Biblioteca.Entidades;
using Lab07.Commands;
using Lab07.Services;

namespace Lab07.ViewModels;

public sealed class ReportsViewModel : PageViewModel
{
    private DateTime? _from = DateTime.Today.AddMonths(-1);
    private DateTime? _to = DateTime.Today;
    public DateTime? From { get => _from; set => Set(ref _from, value); }
    public DateTime? To { get => _to; set => Set(ref _to, value); }
    public ObservableCollection<ReportRow> Rows { get; } = new();
    public AsyncCommand ConsultCommand { get; }
    public ReportsViewModel(IBibliotecaService service) : base(service) => ConsultCommand = new(LoadAsync);
    public override Task LoadAsync()
    {
        if (From is null || To is null || To.Value.Date < From.Value.Date)
        { Error = "Selecciona un intervalo de fechas válido."; return Task.CompletedTask; }
        return RunAsync(async () =>
        {
            var loans = await Service.ReporteAsync(From.Value.Date, To.Value.Date);
            Rows.Clear();
            foreach (var row in loans.SelectMany(p => p.Detalles.Select(d => new ReportRow(p, d))).OrderByDescending(x => x.Date)) Rows.Add(row);
        });
    }
}

public sealed record ReportRow
{
    public ReportRow(Prestamo loan, DetallePrestamo detail)
    {
        LoanId = loan.PrestamoId;
        Member = loan.SocioNombre ?? "—";
        Book = detail.LibroTitulo ?? "—";
        Date = loan.FechaPrestamo;
        Due = loan.FechaLimite;
        StoredStatus = loan.Estado;
        Condition = detail.FechaDevolucion is null && loan.FechaLimite.Date < DateTime.Today ? "Vencido" : detail.FechaDevolucion is null ? "En plazo" : "Devuelto";
    }
    public int LoanId { get; }
    public string Member { get; }
    public string Book { get; }
    public DateTime Date { get; }
    public DateTime Due { get; }
    public string DateText => Date.ToString("dd MMM yyyy");
    public string DueText => Due.ToString("dd MMM yyyy");
    public string StoredStatus { get; }
    public string Condition { get; }
}
