using System.Collections.ObjectModel;
using Biblioteca.Entidades;
using Lab07.Commands;
using Lab07.Services;

namespace Lab07.ViewModels;

public sealed class LoansViewModel : PageViewModel
{
    private Socio? _selectedMember;
    private string _memberSearch = string.Empty;
    private string _bookSearch = string.Empty;
    private int _pendingCount;
    private int _request;
    private CancellationTokenSource? _delay;
    public ObservableCollection<Socio> Members { get; } = new();
    public ObservableCollection<Libro> Books { get; } = new();
    public ObservableCollection<Libro> SelectedBooks { get; } = new();
    public Socio? SelectedMember { get => _selectedMember; set { if (Set(ref _selectedMember, value)) _ = RefreshPendingAsync(); } }
    public string MemberSearch { get => _memberSearch; set { if (Set(ref _memberSearch, value)) Debounce(); } }
    public string BookSearch { get => _bookSearch; set { if (Set(ref _bookSearch, value)) Debounce(); } }
    public int PendingCount { get => _pendingCount; private set { if (Set(ref _pendingCount, value)) Changed(nameof(CapacityText)); } }
    public string CapacityText => SelectedMember is null ? "Selecciona un socio" : $"{PendingCount} pendientes · Puedes agregar {Math.Max(0, 3 - PendingCount)} libro(s)";
    public string SelectionText => $"{SelectedBooks.Count} de {Math.Max(0, 3 - PendingCount)} seleccionados";
    public string DueText => "Negocio asigna la fecha límite al confirmar (14 días).";
    public AsyncCommand RefreshCommand { get; }
    public Func<Task>? DataChanged { get; set; }
    public AsyncCommand ConfirmCommand { get; }
    public RelayCommand AddCommand { get; }
    public RelayCommand RemoveCommand { get; }

    public LoansViewModel(IBibliotecaService service) : base(service)
    {
        RefreshCommand = new(LoadAsync);
        ConfirmCommand = new(ConfirmAsync);
        AddCommand = new(x => { if (x is Libro b && b.Activo && b.Ejemplares > 0 && SelectedBooks.All(y => y.LibroId != b.LibroId) && SelectedBooks.Count + PendingCount < 3) { SelectedBooks.Add(b); Changed(nameof(SelectionText)); } });
        RemoveCommand = new(x => { if (x is Libro b) { SelectedBooks.Remove(b); Changed(nameof(SelectionText)); } });
    }

    private void Debounce()
    {
        _delay?.Cancel();
        var token = (_delay = new CancellationTokenSource()).Token;
        _ = DelayedLoadAsync(token);
    }
    private async Task DelayedLoadAsync(CancellationToken token)
    {
        try { await Task.Delay(300, token); await LoadAsync(); }
        catch (OperationCanceledException) { }
    }

    public Task LoadAsyncCore(int request) => RunAsync(async () =>
    {
        var selected = SelectedMember;
        var members = string.IsNullOrWhiteSpace(MemberSearch) ? await Service.ListarSociosAsync() : await Service.BuscarSociosAsync(MemberSearch.Trim());
        var books = string.IsNullOrWhiteSpace(BookSearch) ? await Service.ListarLibrosAsync() : await Service.BuscarLibrosAsync(BookSearch.Trim());
        if (request != _request) return;
        Members.Clear();
        foreach (var x in members.Where(x => x.Activo)) Members.Add(x);
        if (selected is not null)
        {
            if (Members.All(x => x.SocioId != selected.SocioId)) Members.Insert(0, selected);
            SelectedMember = Members.First(x => x.SocioId == selected.SocioId);
        }
        Books.Clear();
        foreach (var x in books.Where(x => x.Activo)) Books.Add(x);
    });

    public override Task LoadAsync() => LoadAsyncCore(++_request);

    private async Task RefreshPendingAsync()
    {
        Changed(nameof(CapacityText));
        if (SelectedMember is null) { PendingCount = 0; return; }
        var memberId = SelectedMember.SocioId;
        await RunAsync(async () =>
        {
            // The existing report is the only public read contract for pending details.
            var loans = await Service.ReporteAsync(new DateTime(1753, 1, 1), DateTime.Today);
            if (SelectedMember?.SocioId != memberId) return;
            PendingCount = loans.Where(x => x.SocioId == memberId).Sum(x => x.Detalles.Count(d => d.FechaDevolucion is null));
            Changed(nameof(SelectionText));
        });
    }

    private async Task ConfirmAsync()
    {
        if (SelectedMember is null || SelectedBooks.Count == 0) { Error = "Selecciona un socio y al menos un libro."; return; }
        if (PendingCount + SelectedBooks.Count > 3) { Error = "La selección supera la capacidad restante del socio."; return; }
        await RunAsync(async () =>
        {
            var id = await Service.RegistrarPrestamoAsync(SelectedMember.SocioId, SelectedBooks.Select(x => x.LibroId).ToList());
            SelectedBooks.Clear();
            Changed(nameof(SelectionText));
            Notice = $"Préstamo #{id} registrado correctamente.";
            await LoadAsync();
            await RefreshPendingAsync();
            if (DataChanged is not null) await DataChanged();
        });
    }
}
