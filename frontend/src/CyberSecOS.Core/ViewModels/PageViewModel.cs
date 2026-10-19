using CommunityToolkit.Mvvm.ComponentModel;
using CyberSecOS.Core.Api;
using CyberSecOS.Core.Services;

namespace CyberSecOS.Core.ViewModels;

/// <summary>Shared busy/error handling for pages that call the API.</summary>
public abstract partial class PageViewModel : ObservableObject
{
    private readonly SessionService? _session;

    protected PageViewModel(SessionService? session = null)
    {
        _session = session;
    }

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _statusMessage;

    /// <summary>
    /// Runs an API operation, surfacing failures as <see cref="ErrorMessage"/>.
    /// An expired session (401) signs the learner out so the shell returns to login.
    /// </summary>
    protected async Task<bool> RunAsync(Func<Task> operation)
    {
        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = null;
        try
        {
            await operation();
            return true;
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
            if (ex.IsUnauthorized && _session is { IsSignedIn: true })
            {
                _session.SignOut();
            }
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
