using System.Collections.ObjectModel;
using Biblioteca.Entidades;
using Lab07.Commands;
using Lab07.Services;

namespace Lab07.ViewModels;

public sealed class BooksViewModel : PageViewModel
{
    private CancellationTokenSource? _searchDelay;
    private int _request;
    private string _search = string.Empty;
    private string _filter = "Activos";
    private bool _panelOpen;
    private bool _dirty;
    private bool _confirming;
    private Libro? _editing;
    private string _title = string.Empty;
    private string _isbn = string.Empty;
    private string _copies = "1";
    private AuthorChoice? _author;
    private string _titleError = string.Empty;
    private string _isbnError = string.Empty;
    private string _authorError = string.Empty;
    private string _copiesError = string.Empty;

    public ObservableCollection<Libro> Books { get; } = new();
    public ObservableCollection<AuthorChoice> Authors { get; } = new();
    public IReadOnlyList<string> Filters { get; } = new[] { "Activos", "Inactivos", "Todos" };
    public string Search { get => _search; set { if (Set(ref _search, value)) Debounce(); } }
    public string Filter { get => _filter; set { if (Set(ref _filter, value)) _ = LoadAsync(); } }
    public bool PanelOpen { get => _panelOpen; private set => Set(ref _panelOpen, value); }
    public bool Dirty { get => _dirty; private set => Set(ref _dirty, value); }
    public bool Confirming { get => _confirming; private set => Set(ref _confirming, value); }
    public string PanelTitle => _editing is null ? "Nuevo libro" : "Editar libro";
    public bool CanDeactivate => _editing?.Activo == true;
    public string Title { get => _title; set { if (Set(ref _title, value)) Dirty = true; } }
    public string Isbn { get => _isbn; set { if (Set(ref _isbn, value)) Dirty = true; } }
    public string Copies { get => _copies; set { if (Set(ref _copies, value)) Dirty = true; } }
    public AuthorChoice? Author { get => _author; set { if (Set(ref _author, value)) Dirty = true; } }
    public string TitleError { get => _titleError; private set => Set(ref _titleError, value); }
    public string IsbnError { get => _isbnError; private set => Set(ref _isbnError, value); }
    public string AuthorError { get => _authorError; private set => Set(ref _authorError, value); }
    public string CopiesError { get => _copiesError; private set => Set(ref _copiesError, value); }
    public AsyncCommand RefreshCommand { get; }
    public AsyncCommand SaveCommand { get; }
    public AsyncCommand DeactivateCommand { get; }
    public RelayCommand NewCommand { get; }
    public RelayCommand EditCommand { get; }
    public RelayCommand CloseCommand { get; }
    public RelayCommand DiscardCommand { get; }
    public RelayCommand AskDeactivateCommand { get; }
    public RelayCommand CancelDeactivateCommand { get; }

    public BooksViewModel(IBibliotecaService service) : base(service)
    {
        RefreshCommand = new(LoadAsync);
        SaveCommand = new(SaveAsync);
        DeactivateCommand = new(DeactivateAsync);
        NewCommand = new(_ => Open(null));
        EditCommand = new(x => Open(x as Libro));
        CloseCommand = new(_ => ClosePanel());
        DiscardCommand = new(_ => { Dirty = false; ClosePanel(); });
        AskDeactivateCommand = new(_ => Confirming = true);
        CancelDeactivateCommand = new(_ => Confirming = false);
    }

    private void Debounce()
    {
        _searchDelay?.Cancel();
        var cts = _searchDelay = new CancellationTokenSource();
        _ = SearchAfterDelayAsync(cts.Token);
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
            var allBooks = await Service.ListarLibrosAsync();
            var books = string.IsNullOrWhiteSpace(Search)
                ? allBooks
                : await Service.BuscarLibrosAsync(Search.Trim());
            if (request != _request) return;
            Books.Clear();
            foreach (var book in books.Where(x => Filter == "Todos" || (Filter == "Activos" ? x.Activo : !x.Activo))) Books.Add(book);
            var selectedAuthor = Author;
            Authors.Clear();
            foreach (var author in allBooks.Where(x => x.AutorId > 0).GroupBy(x => x.AutorId)
                         .Select(g => new AuthorChoice(g.Key, g.First().AutorNombre ?? $"Autor {g.Key}"))
                         .OrderBy(x => x.Name)) Authors.Add(author);
            if (selectedAuthor is not null)
            {
                if (Authors.All(x => x.Id != selectedAuthor.Id)) Authors.Add(selectedAuthor);
                _author = Authors.First(x => x.Id == selectedAuthor.Id);
                Changed(nameof(Author));
            }
        });
    }

    private void Open(Libro? book)
    {
        _editing = book;
        Title = book?.Titulo ?? string.Empty;
        Isbn = book?.ISBN ?? string.Empty;
        Copies = (book?.Ejemplares ?? 1).ToString();
        Author = Authors.FirstOrDefault(x => x.Id == book?.AutorId);
        Dirty = false;
        Confirming = false;
        Error = string.Empty;
        TitleError = IsbnError = AuthorError = CopiesError = string.Empty;
        PanelOpen = true;
        Changed(nameof(PanelTitle));
        Changed(nameof(CanDeactivate));
    }

    public override bool ClosePanel()
    {
        if (!PanelOpen) return false;
        if (Dirty) { Error = "Hay cambios sin guardar. Usa Cancelar para descartarlos o guarda el formulario."; return true; }
        PanelOpen = false;
        Confirming = false;
        return true;
    }

    public void Discard() { Dirty = false; ClosePanel(); }

    private async Task SaveAsync()
    {
        TitleError = string.IsNullOrWhiteSpace(Title) ? "Escribe un título." : string.Empty;
        IsbnError = string.IsNullOrWhiteSpace(Isbn) ? "Escribe el ISBN." : string.Empty;
        AuthorError = Author is null ? "Selecciona un autor." : string.Empty;
        var validCopies = int.TryParse(Copies, out var copies) && copies >= 0;
        CopiesError = validCopies ? string.Empty : "Escribe un número de ejemplares válido.";
        if (TitleError.Length + IsbnError.Length + AuthorError.Length + CopiesError.Length > 0) return;
        await RunAsync(async () =>
        {
            var book = new Libro { LibroId = _editing?.LibroId ?? 0, Titulo = Title.Trim(), ISBN = Isbn.Trim(), AutorId = Author.Id, Ejemplares = copies };
            if (_editing is null) await Service.CrearLibroAsync(book);
            else await Service.ActualizarLibroAsync(book);
            Dirty = false;
            PanelOpen = false;
            Notice = "Libro guardado correctamente.";
            await LoadAsync();
        });
        if (Error.Contains("ISBN", StringComparison.OrdinalIgnoreCase)) IsbnError = Error;
    }

    private async Task DeactivateAsync()
    {
        if (_editing is null) return;
        await RunAsync(async () =>
        {
            await Service.DarDeBajaLibroAsync(_editing.LibroId);
            Dirty = false;
            PanelOpen = false;
            Confirming = false;
            Notice = "Libro dado de baja. El historial se conserva.";
            await LoadAsync();
        });
    }
}

public sealed record AuthorChoice(int Id, string Name);
