using System.Collections.ObjectModel;
using Biblioteca.Entidades;
using Lab07.Commands;
using Lab07.Services;

namespace Lab07.ViewModels;

public sealed class MembersViewModel : PageViewModel
{
    private CancellationTokenSource? _searchDelay;
    private int _request;
    private string _search = string.Empty;
    private string _filter = "Activos";
    private bool _panelOpen;
    private bool _dirty;
    private bool _confirming;
    private Socio? _editing;
    private string _dni = string.Empty;
    private string _name = string.Empty;
    private string _email = string.Empty;
    private string _dniError = string.Empty;
    private string _nameError = string.Empty;
    private string _emailError = string.Empty;

    public ObservableCollection<Socio> Members { get; } = new();
    public IReadOnlyList<string> Filters { get; } = new[] { "Activos", "Inactivos", "Todos" };
    public string Search { get => _search; set { if (Set(ref _search, value)) Debounce(); } }
    public string Filter { get => _filter; set { if (Set(ref _filter, value)) _ = LoadAsync(); } }
    public bool PanelOpen { get => _panelOpen; private set => Set(ref _panelOpen, value); }
    public bool Dirty { get => _dirty; private set => Set(ref _dirty, value); }
    public bool Confirming { get => _confirming; private set => Set(ref _confirming, value); }
    public string PanelTitle => _editing is null ? "Nuevo socio" : "Editar socio";
    public bool CanDeactivate => _editing?.Activo == true;
    public string Dni { get => _dni; set { if (Set(ref _dni, value)) Dirty = true; } }
    public string Name { get => _name; set { if (Set(ref _name, value)) Dirty = true; } }
    public string Email { get => _email; set { if (Set(ref _email, value)) Dirty = true; } }
    public string DniError { get => _dniError; private set => Set(ref _dniError, value); }
    public string NameError { get => _nameError; private set => Set(ref _nameError, value); }
    public string EmailError { get => _emailError; private set => Set(ref _emailError, value); }
    public AsyncCommand RefreshCommand { get; }
    public AsyncCommand SaveCommand { get; }
    public AsyncCommand DeactivateCommand { get; }
    public RelayCommand NewCommand { get; }
    public RelayCommand EditCommand { get; }
    public RelayCommand CloseCommand { get; }
    public RelayCommand DiscardCommand { get; }
    public RelayCommand AskDeactivateCommand { get; }
    public RelayCommand CancelDeactivateCommand { get; }

    public MembersViewModel(IBibliotecaService service) : base(service)
    {
        RefreshCommand = new(LoadAsync);
        SaveCommand = new(SaveAsync);
        DeactivateCommand = new(DeactivateAsync);
        NewCommand = new(_ => Open(null));
        EditCommand = new(x => Open(x as Socio));
        CloseCommand = new(_ => ClosePanel());
        DiscardCommand = new(_ => { Dirty = false; ClosePanel(); });
        AskDeactivateCommand = new(_ => Confirming = true);
        CancelDeactivateCommand = new(_ => Confirming = false);
    }

    private void Debounce()
    {
        _searchDelay?.Cancel();
        var token = (_searchDelay = new CancellationTokenSource()).Token;
        _ = SearchAfterDelayAsync(token);
    }
    private async Task SearchAfterDelayAsync(CancellationToken token)
    {
        try { await Task.Delay(300, token); await LoadAsync(); }
        catch (OperationCanceledException) { }
    }

    public override async Task LoadAsync()
    {
        var request = ++_request;
        await RunAsync(async () =>
        {
            var members = string.IsNullOrWhiteSpace(Search)
                ? await Service.ListarSociosAsync()
                : await Service.BuscarSociosAsync(Search.Trim());
            if (request != _request) return;
            Members.Clear();
            foreach (var member in members.Where(x => Filter == "Todos" || (Filter == "Activos" ? x.Activo : !x.Activo))) Members.Add(member);
        });
    }

    private void Open(Socio? member)
    {
        _editing = member;
        Dni = member?.DNI ?? string.Empty;
        Name = member?.Nombre ?? string.Empty;
        Email = member?.Email ?? string.Empty;
        Dirty = false;
        Confirming = false;
        Error = string.Empty;
        DniError = NameError = EmailError = string.Empty;
        PanelOpen = true;
        Changed(nameof(PanelTitle));
        Changed(nameof(CanDeactivate));
    }

    public override bool ClosePanel()
    {
        if (!PanelOpen) return false;
        if (Dirty) { Error = "Hay cambios sin guardar. Usa Descartar o guarda el formulario."; return true; }
        PanelOpen = false;
        Confirming = false;
        return true;
    }

    private async Task SaveAsync()
    {
        DniError = string.IsNullOrWhiteSpace(Dni) ? "Escribe el DNI." : string.Empty;
        NameError = string.IsNullOrWhiteSpace(Name) ? "Escribe el nombre." : string.Empty;
        EmailError = string.IsNullOrWhiteSpace(Email) ? "Escribe el correo." : string.Empty;
        if (DniError.Length + NameError.Length + EmailError.Length > 0) return;
        await RunAsync(async () =>
        {
            var member = new Socio { SocioId = _editing?.SocioId ?? 0, DNI = Dni.Trim(), Nombre = Name.Trim(), Email = Email.Trim() };
            if (_editing is null) await Service.CrearSocioAsync(member);
            else await Service.ActualizarSocioAsync(member);
            Dirty = false;
            PanelOpen = false;
            Notice = "Socio guardado correctamente.";
            await LoadAsync();
        });
        if (Error.Contains("DNI", StringComparison.OrdinalIgnoreCase)) DniError = Error;
    }

    private async Task DeactivateAsync()
    {
        if (_editing is null) return;
        await RunAsync(async () =>
        {
            await Service.DarDeBajaSocioAsync(_editing.SocioId);
            Dirty = false;
            PanelOpen = false;
            Confirming = false;
            Notice = "Socio dado de baja. El historial se conserva.";
            await LoadAsync();
        });
    }
}
