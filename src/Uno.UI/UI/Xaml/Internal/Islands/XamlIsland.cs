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

	internal ContentManager ContentManager => _contentManager;

	public UIElement? Content
	{
		get => _contentManager.Content;
		set
		{
			_contentManager.Content = value;

			SetPublicRootVisual(value, null, null);

			ContentManager.TryLoadRootVisual(XamlRoot!);
		}
	}

	internal Window? OwnerWindow { get; set; }

	private void XamlIsland_SizeChanged(object sender, SizeChangedEventArgs args) => Content?.XamlRoot?.NotifyChanged();
}
