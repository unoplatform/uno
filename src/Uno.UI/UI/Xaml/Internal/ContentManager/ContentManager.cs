#nullable enable

using System;
using Uno.UI.Xaml.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;

namespace Uno.UI.Xaml.Controls;

internal partial class ContentManager
{
	private readonly object _owner;
	private readonly bool _isCoreWindowContent;

	private UIElement? _content;
	private UIElement? _privateRootElement;
	private UIElement? _publicRootElement;
	private RootVisual? _rootVisual;

	private UIElement? _rootScrollViewer;

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

	public UIElement? PublicRootElement => _publicRootElement;

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

		if (_rootVisual is null)
		{
			if (_isCoreWindowContent)
			{
				WinUICoreServices.Instance.PutCoreWindowVisualRoot(_publicRootElement);
				_rootVisual = WinUICoreServices.Instance.MainRootVisual;

				if (_rootVisual?.XamlRoot is null)
				{
					throw new InvalidOperationException("The root visual was not created.");
				}
			}
		}

		if (_isCoreWindowContent)
		{
			TryLoadRootVisual();
		}

		oldContent?.XamlRoot?.NotifyChanged();
		if (newContent?.XamlRoot != oldContent?.XamlRoot)
		{
			newContent?.XamlRoot?.NotifyChanged();
		}
	}

#if __WASM__ // Only WASM currently has a root scroll viewer.
	private void CreateRootScrollViewer(UIElement content)
	{
		var rootBorder = new Border();
		RootScrollViewer = new ScrollViewer()
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
			VerticalScrollMode = ScrollMode.Disabled,
			HorizontalScrollMode = ScrollMode.Disabled,
			Content = rootBorder
		};
		rootBorder.Child = _content = content;
	}
#endif
}
