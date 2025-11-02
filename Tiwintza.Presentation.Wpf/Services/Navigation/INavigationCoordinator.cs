using System;

namespace Tiwintza.Presentation.Wpf.Services.Navigation;

public interface INavigationCoordinator
{
    event EventHandler<NavigationRequestEventArgs>? NavigationRequested;
    void Navigate(string key, Action<object?>? afterNavigate = null);
}
