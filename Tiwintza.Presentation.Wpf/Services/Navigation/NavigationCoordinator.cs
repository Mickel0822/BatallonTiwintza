using System;

namespace Tiwintza.Presentation.Wpf.Services.Navigation;

public sealed class NavigationCoordinator : INavigationCoordinator
{
    public event EventHandler<NavigationRequestEventArgs>? NavigationRequested;

    public void Navigate(string key, Action<object?>? afterNavigate = null)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key requerido", nameof(key));
        NavigationRequested?.Invoke(this, new NavigationRequestEventArgs(key, afterNavigate));
    }
}
