#nullable enable

using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUICoreServices = global::Uno.UI.Xaml.Core.CoreServices;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Content;

namespace Uno.UI.Xaml.Islands;

internal partial class XamlIslandRoot : Panel
{
	private readonly ContentManager _contentManager;

	public XamlIslandRoot()
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
