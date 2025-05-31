﻿#nullable enable

using System;
using Uno.UI.Xaml.Core;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;
using Uno.Disposables;
using Uno.UI.Xaml.Islands;

namespace Uno.UI.Xaml.Controls;

internal partial class ContentManager
{
	private readonly object _owner;
	private readonly bool _isCoreWindowContent;

	private UIElement? _content;
	private RootVisual? _rootVisual;
	private readonly SerialDisposable _contentLoadedDisposable = new();

	public ContentManager(object owner, bool isCoreWindowContent)
	{
		_owner = owner;
		_isCoreWindowContent = isCoreWindowContent;
	}

	public UIElement? Content
	{
		get => _content;
		set => SetContent(value);
	}

	internal Microsoft.UI.Xaml.Controls.ScrollViewer? RootScrollViewer { get; private set; }

	private void SetContent(UIElement? newContent)
	{
		var oldContent = Content;

		if (oldContent == newContent)
		{
			// Content already set, ignore.
			return;
		}

		_contentLoadedDisposable.Disposable = null;

		if (_isCoreWindowContent)
		{
			if (WinUICoreServices.Instance.MainVisualTree is not { } visualTree)
			{
				throw new InvalidOperationException("Main visual tree must be initialized");
			}
			// TODO: Add RootScrollViewer everywhere
			visualTree.SetPublicRootVisual(newContent, rootScrollViewer: null, rootContentPresenter: null);

#if UNO_HAS_ENHANCED_LIFECYCLE
			WinUICoreServices.Instance.RaisePendingLoadedRequests();
#endif

			if (_rootVisual is null)
			{
				// Initialize root visual and attach it to owner window.

				_rootVisual = WinUICoreServices.Instance.MainRootVisual;

				if (_rootVisual?.XamlRoot is null)
				{
					throw new InvalidOperationException("The root visual was not created.");
				}

				if (_owner is not Microsoft.UI.Xaml.Window window)
				{
					throw new InvalidOperationException("Owner of ContentManager should be a Window");
				}

				AttachToWindow(_rootVisual, window);
			}

			// For an unknown reason we need to make sure to reset the Frame of the root view controller on iOS when Content changes,
			// otherwise EVP and When_Mask_All tests fail as the viewport will extend under status bar. This should be investigated in #8978.
#if __APPLE_UIKIT__
			if (_owner is not Microsoft.UI.Xaml.Window windowOuter)
			{
				throw new InvalidOperationException("Owner of ContentManager should be a Window");
			}

			AttachToWindow(_rootVisual, windowOuter);
#endif
		}

		if (newContent is FrameworkElement frameworkElement)
		{
			frameworkElement.Loaded += FrameworkElement_Loaded;

			_contentLoadedDisposable.Disposable = Disposable.Create(() =>
			{
				frameworkElement.Loaded -= FrameworkElement_Loaded;
			});
		}
		else if (newContent is not null)
		{
			NotifyContentLoaded();
		}

		_content = newContent;
	}

	private void FrameworkElement_Loaded(object sender, RoutedEventArgs e)
	{
		_contentLoadedDisposable.Disposable = null;
		NotifyContentLoaded();
	}

	private void NotifyContentLoaded()
	{
		if (_owner is Window window)
		{
			window.NotifyContentLoaded();
		}
		else if (_owner is XamlIslandRoot xamlIslandRoot)
		{
			xamlIslandRoot.OwnerWindow?.NotifyContentLoaded();
		}
		else
		{
			throw new InvalidOperationException("Owner of ContentManager should be a Window or XamlIslandRoot");
		}
	}

	internal static void TryLoadRootVisual(XamlRoot xamlRoot)
	{
		if (!xamlRoot.IsHostVisible)
		{
			return;
		}

		if (xamlRoot?.VisualTree?.RootElement is not FrameworkElement rootElement ||
			rootElement.IsLoaded)
		{
			return;
		}

#if !IS_UNIT_TESTS
		// Even if we're on the main thread, we need to delay this enough so that the window is initialized (i.e. Application._initializationComplete is true)
		_ = rootElement.Dispatcher.RunAsync(CoreDispatcherPriority.High, () => LoadRootElementPlatform(xamlRoot, rootElement));
#else
		var dispatcher = rootElement.Dispatcher;

		if (dispatcher.HasThreadAccess)
		{
			LoadRootElementPlatform(xamlRoot, rootElement);
		}
		else
		{
			_ = dispatcher.RunAsync(CoreDispatcherPriority.High, () => LoadRootElementPlatform(xamlRoot, rootElement));
		}
#endif
	}

	static partial void LoadRootElementPlatform(XamlRoot xamlRoot, UIElement rootElement);

	internal static void AttachToWindow(UIElement rootElement, Microsoft.UI.Xaml.Window window) => AttachToWindowPlatform(rootElement, window);

	static partial void AttachToWindowPlatform(UIElement rootElement, Microsoft.UI.Xaml.Window window);
}
