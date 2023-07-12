#nullable enable

using Uno.UI.Xaml.Core;
using Windows.UI.Xaml;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;

namespace Uno.UI.Xaml.Controls;

internal partial class CoreWindowWindow : IWindowImplementation
{
	private readonly Window _window;
	private readonly ContentManager _contentManager;

	public CoreWindowWindow(Window window)
	{
		_window = window;
		_contentManager = new ContentManager(_window, true);
	}

	public UIElement? Content
	{
		get
		{
			if (WinUICoreServices.Instance.InitializationType == InitializationType.IslandsOnly)
			{
				// In case of Islands, Window.Current.Content just returns null.
				return null;
			}

			return _contentManager.Content;
		}
		set => _contentManager.Content = value;
	}
}
