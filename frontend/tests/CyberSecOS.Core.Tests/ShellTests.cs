using System.Net;
using CyberSecOS.Core.Services;
using CyberSecOS.Core.Tests.Fakes;
using CyberSecOS.Core.ViewModels;

namespace CyberSecOS.Core.Tests;

public class ShellTests
{
    private readonly FakeApi _api = new();
    private readonly SessionService _session;
    private readonly NavigationService _navigation;
    private readonly MainViewModel _shell;

    public ShellTests()
    {
        _session = new SessionService(_api);
        _navigation = new NavigationService(Create);
        _shell = new MainViewModel(_navigation, _session);
    }

    private object Create(Type type) => type.Name switch
    {
        nameof(LoginViewModel) => new LoginViewModel(_api, _session),
        nameof(ProfileViewModel) => new ProfileViewModel(_api, _session),
        nameof(CoursesViewModel) => new CoursesViewModel(_api, _navigation, _session),
        _ => throw new InvalidOperationException(type.Name),
    };

    [Fact]
    public void StartsOnLoginPage()
    {
        _shell.Start();

        Assert.IsType<LoginViewModel>(_shell.CurrentPage);
        Assert.False(_shell.IsSignedIn);
    }

    [Fact]
    public void SigningInOpensHomePage()
    {
        _shell.Start();

        _session.SignIn(FakeApi.Token(FakeApi.Adam));

        Assert.True(_shell.IsSignedIn);
        Assert.Equal("Adam", _shell.UserDisplayName);
        Assert.IsType(_shell.HomePage, _shell.CurrentPage);
        Assert.Equal(_shell.HomePage, _shell.SelectedItem?.PageType);
    }

    [Fact]
    public void SelectingNavigationItemChangesPage()
    {
        _shell.Start();
        _session.SignIn(FakeApi.Token(FakeApi.Adam));

        _shell.SelectedItem = _shell.NavigationItems.Single(i => i.PageType == typeof(ProfileViewModel));

        Assert.IsType<ProfileViewModel>(_shell.CurrentPage);
    }

    [Fact]
    public void SignOutReturnsToLoginAndDropsToken()
    {
        _shell.Start();
        _session.SignIn(FakeApi.Token(FakeApi.Adam));

        _shell.SignOutCommand.Execute(null);

        Assert.IsType<LoginViewModel>(_shell.CurrentPage);
        Assert.Null(_api.AccessToken);
        Assert.Null(_shell.SelectedItem);
    }

    [Fact]
    public async Task ExpiredSessionSignsOut()
    {
        _shell.Start();
        _session.SignIn(FakeApi.Token(FakeApi.Adam));
        _api.Courses = () => throw FakeApi.Error(HttpStatusCode.Unauthorized, "Not authenticated");

        await ((IPageViewModel)_shell.CurrentPage!).OnNavigatedToAsync();

        Assert.False(_shell.IsSignedIn);
        Assert.IsType<LoginViewModel>(_shell.CurrentPage);
    }
}
