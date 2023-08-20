#nullable enable

using System;
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
		if (CoreServices.Instance.MainVisualTree is null)
		{
			throw new InvalidOperationException("Main visual tree is not initialized.");
		}
		_contentManager.TryLoadRootVisual(CoreServices.Instance.MainVisualTree.GetOrCreateXamlRoot());
	}
}
