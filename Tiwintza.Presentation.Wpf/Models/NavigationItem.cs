namespace Tiwintza.Presentation.Wpf.Models
{
    public sealed class NavigationItem
    {
        public string Key { get; }
        public string Title { get; }
        public string IconKind { get; } // nombre de PackIconMaterialKind

        public NavigationItem(string key, string title, string iconKind)
            => (Key, Title, IconKind) = (key, title, iconKind);

        public override string ToString() => Title;
    }
}
