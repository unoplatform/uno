#nullable enable

using System;
using Uno.UI.Xaml.Core;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;

namespace Uno.UI.Xaml.Controls;

internal partial class ContentManager
{
	private readonly object _owner;
	private readonly bool _isCoreWindowContent;

	private UIElement? _content;
	private RootVisual? _rootVisual;

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

	internal Windows.UI.Xaml.Controls.ScrollViewer? RootScrollViewer { get; private set; }

	private void RootSizeChanged(object sender, SizeChangedEventArgs args) => _rootVisual?.XamlRoot?.NotifyChanged();

	private void SetContent(UIElement? newContent)
	{
		var oldContent = Content;

		if (oldContent == newContent)
		{
			// Content already set, ignore.
			return;
		}

		if (_isCoreWindowContent)
		{
			if (WinUICoreServices.Instance.MainVisualTree is not { } visualTree)
			{
				throw new InvalidOperationException("Main visual tree must be initialized");
			}
			// TODO:MZ: Add RootScrollViewer everywhere
			visualTree.SetPublicRootVisual(newContent, rootScrollViewer: null, rootContentPresenter: null);
			_rootVisual = WinUICoreServices.Instance.MainRootVisual;

			if (_rootVisual?.XamlRoot is null)
			{
				throw new InvalidOperationException("The root visual was not created.");
			}

			SetupCoreWindowRootVisualPlatform(_rootVisual);
		}

		_content = newContent;
	}

	partial void SetupCoreWindowRootVisualPlatform(RootVisual rootVisual);

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

		var dispatcher = rootElement.Dispatcher;

		if (dispatcher.HasThreadAccess)
		{
			LoadRootElementPlatform(xamlRoot, rootElement);
		}
		else
		{
			_ = dispatcher.RunAsync(CoreDispatcherPriority.High, () => LoadRootElementPlatform(xamlRoot, rootElement));
		}
	}

	static partial void LoadRootElementPlatform(XamlRoot xamlRoot, UIElement rootElement);
}
