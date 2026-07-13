#nullable enable

using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUICoreServices = global::Uno.UI.Xaml.Core.CoreServices;
using Microsoft.UI.Xaml.Media;

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

#if __SKIA__
	private RootScrollViewer? _rootScrollViewer;

	private RootScrollViewer EnsureRootScrollViewer()
	{
		_rootScrollViewer ??= new RootScrollViewer();
		return _rootScrollViewer;
	}
#endif

	public UIElement? Content
	{
		get => _contentManager.Content;
		set
		{
			_contentManager.Content = value;

#if __SKIA__
			var rootScrollViewer = EnsureRootScrollViewer();
			rootScrollViewer.Content = value;
			SetPublicRootVisual(value, rootScrollViewer, null);
#else
			SetPublicRootVisual(value, null, null);
#endif
		}
	}

	internal Window? OwnerWindow { get; set; }

	internal bool IsSiteVisible { get; set; }
}
