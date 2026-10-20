using System.Windows.Controls;
using System.Windows.Threading;
using CyberSecOS.Core.ViewModels;

namespace CyberSecOS.Desktop.Views;

public partial class ExamsView : UserControl
{
    private readonly DispatcherTimer _timer;

    public ExamsView()
    {
        InitializeComponent();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => (DataContext as ExamsViewModel)?.UpdateElapsed();
        Loaded += (_, _) => _timer.Start();
        Unloaded += (_, _) => _timer.Stop();
    }
}
