using System.ComponentModel;
using System.Runtime.ExceptionServices;
using System.Windows.Controls;
using System.Windows.Data;
using CyberSecOS.Desktop.Behaviors;

namespace CyberSecOS.Desktop.Tests;

/// <summary>
/// The sign-in and account-setup pages bind PasswordBox through this attached
/// property. The view models start with an empty password, which once left typing
/// unbound (Create account stayed disabled), so these run on an STA thread with a
/// real PasswordBox.
/// </summary>
public class PasswordBoxAssistantTests
{
    private sealed class Form : INotifyPropertyChanged
    {
        private string _password = string.Empty;

        public string Password
        {
            get => _password;
            set { _password = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Password))); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    private static void RunOnSta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }

    private static PasswordBox Bind(Form form)
    {
        var box = new PasswordBox();
        BindingOperations.SetBinding(box, PasswordBoxAssistant.BoundPasswordProperty,
            new Binding(nameof(Form.Password)) { Source = form, Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
        return box;
    }

    [Fact]
    public void Typing_reaches_a_view_model_that_starts_empty() => RunOnSta(() =>
    {
        var form = new Form();
        var box = Bind(form);

        box.Password = "correct horse 1";

        Assert.Equal("correct horse 1", form.Password);
    });

    [Fact]
    public void Clearing_the_view_model_clears_the_box() => RunOnSta(() =>
    {
        var form = new Form();
        var box = Bind(form);
        box.Password = "secret123x";

        form.Password = string.Empty;

        Assert.Equal(string.Empty, box.Password);
    });
}
