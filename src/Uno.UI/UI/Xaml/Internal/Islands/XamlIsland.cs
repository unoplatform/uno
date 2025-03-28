#nullable enable

using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using WinUICoreServices = global::Uno.UI.Xaml.Core.CoreServices;
using Windows.UI.Xaml.Media;

namespace Uno.UI.Xaml.Islands;

internal partial class XamlIsland : Panel
{
	private readonly ContentManager _contentManager;

	public XamlIsland()
	{
		_contentManager = new(this, false);
		// TODO: Uno specific - additional root logic required by Uno.
		_rootElementLogic = new(this);

		InitializeRoot(WinUICoreServices.Instance);
	}

	internal ContentManager ContentManager => _contentManager;

	public UIElement? Content
	{
		get => _contentManager.Content;
		set
		{
			_contentManager.Content = value;

			SetPublicRootVisual(value, null, null);
		}
	}

	internal Window? OwnerWindow { get; set; }

	internal bool IsSiteVisible { get; set; }
}
