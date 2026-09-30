using Lab07.Commands;
using Lab07.Services;

namespace Lab07.ViewModels;

public sealed class ShellViewModel : ObservableObject
{
    private PageViewModel _current;
    private string _section = "Inicio";
    private string _subtitle = "Tu biblioteca, en orden y a mano.";
    private readonly Dictionary<string, PageViewModel> _pages;
    public HomeViewModel Home { get; }
    public BooksViewModel Books { get; }
    public MembersViewModel Members { get; }
    public LoansViewModel Loans { get; }
    public ReturnsViewModel Returns { get; }
    public ReportsViewModel Reports { get; }
    public PageViewModel Current { get => _current; private set => Set(ref _current, value); }
    public string Section { get => _section; private set => Set(ref _section, value); }
    public string Subtitle { get => _subtitle; private set => Set(ref _subtitle, value); }
    public bool IsHome => Section == "Inicio";
    public bool IsBooks => Section == "Libros";
    public bool IsMembers => Section == "Socios";
    public bool IsLoans => Section == "Préstamos";
    public bool IsReturns => Section == "Devoluciones";
    public bool IsReports => Section == "Reportes";
    public RelayCommand NavigateCommand { get; }

    public ShellViewModel() : this(new UnavailableService()) { }

    public ShellViewModel(IBibliotecaService service)
    {
        Home = new(service); Books = new(service); Members = new(service);
        Loans = new(service); Returns = new(service); Reports = new(service);
        Loans.DataChanged = RefreshSummaryAsync;
        Returns.DataChanged = RefreshSummaryAsync;
        _pages = new() { ["Inicio"] = Home, ["Libros"] = Books, ["Socios"] = Members,
            ["Préstamos"] = Loans, ["Devoluciones"] = Returns, ["Reportes"] = Reports };
        _current = Home;
        NavigateCommand = new(x => { if (x is string name) Navigate(name); });
        _ = Home.LoadAsync();
    }

    public void Navigate(string section)
    {
        if (!_pages.TryGetValue(section, out var page)) return;
        Current = page;
        Section = section;
        Changed(nameof(IsHome)); Changed(nameof(IsBooks)); Changed(nameof(IsMembers));
        Changed(nameof(IsLoans)); Changed(nameof(IsReturns)); Changed(nameof(IsReports));
        Subtitle = section switch
        {
            "Libros" => "Encuentra y cuida cada ejemplar.",
            "Socios" => "Personas que hacen viva esta biblioteca.",
            "Préstamos" => "Una selección clara, un solo registro.",
            "Devoluciones" => "Cada libro vuelve a su lugar.",
            "Reportes" => "Consulta el movimiento de la biblioteca.",
            _ => "Tu biblioteca, en orden y a mano."
        };
        _ = page.LoadAsync();
    }

    public bool ClosePanel() => Current.ClosePanel();

    private async Task RefreshSummaryAsync()
    {
        await Home.LoadAsync();
        await Books.LoadAsync();
    }
}
