#nullable enable

using Windows.Foundation;

namespace Windows.UI.Xaml
{
	public partial class XamlRoot
	{
		internal static XamlRoot Current { get; } = new XamlRoot();

		public event TypedEventHandler<XamlRoot, XamlRootChangedEventArgs>? Changed;

		public UIElement? Content => Window.Current?.Content;

		public Size Size => Content?.RenderSize ?? Size.Empty;

		internal void NotifyChanged()
		{
			Changed?.Invoke(this, new XamlRootChangedEventArgs());
		}
	}
}
