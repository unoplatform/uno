#nullable enable

using System;
using Uno.UI.Xaml.Core;
using Windows.UI.Xaml;
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

#if __WASM__ // Only WASM currently has a root scroll viewer.
		CreateRootScrollViewer(newContent);
#endif

		if (_isCoreWindowContent && _rootVisual is null)
		{
			WinUICoreServices.Instance.PutCoreWindowVisualRoot(newContent);
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

#if __WASM__ // Only WASM currently has a root scroll viewer.
	private void CreateRootScrollViewer(UIElement content)
	{
		var rootBorder = new Border()
		{
			Child = content;
		}

		RootScrollViewer = new ScrollViewer()
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
			VerticalScrollMode = ScrollMode.Disabled,
			HorizontalScrollMode = ScrollMode.Disabled,
			Content = rootBorder
		};
	}
#endif

#if !__SKIA__
	internal static void TryLoadRootVisual(XamlRoot xamlRoot)
	{
		//TODO:MZ: Implement this for all platforms!
	}
#endif
}
