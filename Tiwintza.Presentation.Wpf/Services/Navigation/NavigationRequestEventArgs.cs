using System;

namespace Tiwintza.Presentation.Wpf.Services.Navigation;

public sealed class NavigationRequestEventArgs : EventArgs
{
    public NavigationRequestEventArgs(string key, Action<object?>? afterNavigate)
    {
        Key = key;
        AfterNavigate = afterNavigate;
    }

    public string Key { get; }
    public Action<object?>? AfterNavigate { get; }
}
