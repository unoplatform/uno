#nullable enable

using Uno.UI.Xaml.Core;
using Windows.UI.Core;
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
		CoreWindow = CoreWindow.GetOrCreateForCurrentThread();
	}

#pragma warning disable CS0067
	public event SizeChangedEventHandler? SizeChanged;

	public CoreWindow CoreWindow { get; }

	public bool Visible => CoreWindow.Visible;

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

	public void Activate()
	{
		// Currently Uno supports only single window,
		// for compatibility with WinUI we set the first activated
		// as Current #8341
		_wasActivated = true;

		// Initialize visibility on first activation.			
		Visible = true;

		TryShow();

		OnNativeActivated(CoreWindowActivationState.CodeActivated);
	}
}
