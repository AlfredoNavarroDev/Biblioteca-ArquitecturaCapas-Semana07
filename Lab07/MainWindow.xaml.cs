using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Lab07.Services;
using Lab07.ViewModels;

namespace Lab07;

public partial class MainWindow : Window
{
    public MainWindow() : this(new UnavailableService()) { }

    public MainWindow(IBibliotecaService service)
    {
        InitializeComponent();
        var shell = new ShellViewModel(service);
        DataContext = shell;
        shell.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName != nameof(ShellViewModel.Current)) return;
            var animationsEnabled = typeof(SystemParameters).GetProperty("ClientAreaAnimation")?.GetValue(null) is true;
            if (animationsEnabled)
                PageHost.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(160)));
            else
                PageHost.Opacity = 1;
        };
    }

    private void OnEscape(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || DataContext is not ShellViewModel shell) return;
        if (shell.ClosePanel()) e.Handled = true;
    }
}
