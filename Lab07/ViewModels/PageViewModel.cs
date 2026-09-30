using Biblioteca.Negocio;
using Lab07.Services;

namespace Lab07.ViewModels;

public abstract class PageViewModel : ObservableObject
{
    protected readonly IBibliotecaService Service;
    private bool _isLoading;
    private string _error = string.Empty;
    private string _notice = string.Empty;
    private CancellationTokenSource? _noticeDelay;

    protected PageViewModel(IBibliotecaService service) => Service = service;
    public bool IsLoading { get => _isLoading; protected set => Set(ref _isLoading, value); }
    public string Error { get => _error; protected set => Set(ref _error, value); }
    public string Notice
    {
        get => _notice;
        protected set
        {
            if (!Set(ref _notice, value)) return;
            _noticeDelay?.Cancel();
            if (value.Length > 0) _ = ClearNoticeLaterAsync(_noticeDelay = new CancellationTokenSource());
        }
    }

    private async Task ClearNoticeLaterAsync(CancellationTokenSource delay)
    {
        try { await Task.Delay(TimeSpan.FromSeconds(4), delay.Token); Notice = string.Empty; }
        catch (OperationCanceledException) { }
        finally { if (ReferenceEquals(_noticeDelay, delay)) _noticeDelay = null; delay.Dispose(); }
    }

    protected async Task RunAsync(Func<Task> action)
    {
        IsLoading = true;
        Error = string.Empty;
        try { await action(); }
        catch (ReglaNegocioException ex) { Error = ex.Message; }
        catch (InvalidOperationException ex) when (Service is UnavailableService) { Error = ex.Message; }
        catch { Error = "No se pudo completar la operación. Revisa la conexión e inténtalo de nuevo."; }
        finally { IsLoading = false; }
    }

    public abstract Task LoadAsync();
    public virtual bool ClosePanel() => false;
}
