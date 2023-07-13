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
	private UIElement? _rootElement;
	private RootVisual? _rootVisual;
	private Border? _rootBorder;

	public ContentManager(object owner, bool isCoreWindowContent)
	{
		_owner = owner;
		_isCoreWindowContent = isCoreWindowContent;
	}

	public UIElement? Content
	{
		get => _content;
		set => InternalSetContent(value);
	}

	public UIElement? RootElement => _rootElement;

	private void InternalSetContent(UIElement? content)
	{
		//if (WinUICoreServices.Instance.InitializationType == InitializationType.IslandsOnly)
		//{
		//	// Ignore setter, in line with XAML Islands behavior.
		//	return;
		//}

		//if (Content == value)
		//{
		//	// Content already set, ignore.
		//	return;
		//}

		//var oldContent = Content;
		//if (oldContent != null)
		//{
		//	oldContent.IsWindowRoot = false;

		//	if (oldContent is FrameworkElement oldRoot)
		//	{
		//		oldRoot.SizeChanged -= RootSizeChanged;
		//	}
		//}

		//if (value is not null)
		//{
		//	value.IsWindowRoot = true;
		//}

		//InternalSetContent(value);

		//if (value is FrameworkElement newRoot)
		//{
		//	newRoot.SizeChanged += RootSizeChanged;
		//}

		//oldContent?.XamlRoot?.NotifyChanged();
		//if (value?.XamlRoot != oldContent?.XamlRoot)
		//{
		//	value?.XamlRoot?.NotifyChanged();
		//}

		//TryShow();


		if (_content == content)
		{
			return;
		}

		if (_rootVisual is null)
		{
			_rootBorder = new Border();
			_rootElement = _rootBorder;
#if __WASM__ // Only WASM currently has a root scroll viewer.
			_rootScrollViewer = new ScrollViewer()
			{
				VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
				HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
				VerticalScrollMode = ScrollMode.Disabled,
				HorizontalScrollMode = ScrollMode.Disabled,
				Content = _rootBorder
			};
			//TODO Uno: We can set and RootScrollViewer properly in case of WASM
			_rootElement = _rootScrollViewer;
#endif

			if (_isCoreWindowContent)
			{
				WinUICoreServices.Instance.PutVisualRoot(_rootElement);
				_rootVisual = WinUICoreServices.Instance.MainRootVisual;

				if (_rootVisual?.XamlRoot is null)
				{
					throw new InvalidOperationException("The root visual was not created.");
				}
			}
		}

		//// Attaching to window?

		if (_rootBorder != null)
		{
			_rootBorder.Child = _content = content;
		}

		if (_isCoreWindowContent)
		{
			TryLoadRootVisual();
		}
	}
}
