#nullable enable

using Uno.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using WinUICoreServices = global::Uno.UI.Xaml.Core.CoreServices;

namespace Uno.UI.Xaml.Islands;

internal partial class XamlIsland : Panel
{
	private readonly ContentManager _contentManager;

	public XamlIsland()
	{
		_contentManager = new(this, false);
		InitializeRoot(WinUICoreServices.Instance);
		SizeChanged += XamlIsland_SizeChanged;
	}

	private void XamlIsland_SizeChanged(object sender, SizeChangedEventArgs args) => Content?.XamlRoot?.NotifyChanged();

	public UIElement? Content
	{
		get => _contentManager.Content;
		set
		{
			_contentManager.Content = value;

			SetPublicRootVisual(value, null, null);

			UIElement.LoadingRootElement(_contentRoot.VisualTree.RootElement!);

			_contentRoot.VisualTree.RootElement!.XamlRoot!.InvalidateMeasure();
			_contentRoot.VisualTree.RootElement!.XamlRoot!.InvalidateArrange();

			UIElement.RootElementLoaded(_contentRoot.VisualTree.RootElement!);
		}
	}

	internal Window? Window { get; set; }
}
