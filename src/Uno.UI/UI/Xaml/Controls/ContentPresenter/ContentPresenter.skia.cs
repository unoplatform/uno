using System;
using System.Runtime.InteropServices;
using Uno.Extensions;
using Uno.Foundation.Extensibility;
using Uno.UI;
using Uno.UI.DataBinding;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

partial class ContentPresenter
{
	private static readonly Lazy<INativeElementHostingExtension> _nativeElementHostingExtension = new Lazy<INativeElementHostingExtension>(() =>
	{
		ApiExtensibility.CreateInstance<INativeElementHostingExtension>(typeof(ContentPresenter), out var extension);
		return extension;
	});

	private Rect? _lastArrangeRect;
	private Rect _lastGlobalRect;
	private bool _nativeHostRegistered;

	partial void InitializePlatform()
	{
		Loaded += (s, e) => RegisterNativeHostSupport();
		Unloaded += (s, e) => UnregisterNativeHostSupport();
	}

	partial void TryRegisterNativeElement(object newValue)
	{
		if (IsNativeElement(newValue))
		{
			IsNativeHost = true;

			if (ContentTemplate is not null)
			{
				throw new InvalidOperationException("ContentTemplate cannot be set when the Content is a native element");
			}
			if (ContentTemplateSelector is not null)
			{
				throw new InvalidOperationException("ContentTemplateSelector cannot be set when the Content is a native element");
			}

			RegisterNativeHostSupport();
		}
		else if (IsNativeHost)
		{
			IsNativeHost = false;
			UnregisterNativeHostSupport();
		}
	}

	void RegisterNativeHostSupport()
	{
		if (IsNativeHost && XamlRoot is not null)
		{
			XamlRoot.InvalidateRender += UpdateNativeElementPosition;
			_nativeHostRegistered = true;
		}
	}

	void UnregisterNativeHostSupport()
	{
		if (_nativeHostRegistered)
		{
			_nativeHostRegistered = false;
			XamlRoot.InvalidateRender -= UpdateNativeElementPosition;
		}
	}

	partial void ArrangeNativeElement(Rect arrangeRect)
	{
		if (IsNativeHost)
		{
			_lastArrangeRect = arrangeRect;

			UpdateNativeElementPosition();
		}
	}

	partial void TryAttachNativeElement()
	{
		if (IsNativeHost)
		{
			AttachNativeElement(XamlRoot, Content);
		}
	}

	partial void TryDetachNativeElement()
	{
		if (IsNativeHost)
		{
			DetachNativeElement(XamlRoot, Content);
		}
	}

	private Size MeasureNativeElement(Size size)
	{
		if (IsNativeHost)
		{
			return MeasureNativeElement(XamlRoot, Content, size);
		}
		else
		{
			return size;
		}
	}

	private void UpdateNativeElementPosition()
	{
		if (_lastArrangeRect is { } lastArrangeRect)
		{
			var globalPosition = TransformToVisual(null).TransformPoint(lastArrangeRect.Location);
			var globalRect = new Rect(globalPosition, lastArrangeRect.Size);

			if (_lastGlobalRect != globalRect)
			{
				_lastGlobalRect = globalRect;

				_nativeElementHostingExtension.Value?.ArrangeNativeElement(XamlRoot, Content, globalRect);
			}
		}
	}

	internal static bool IsNativeElement(object content) => _nativeElementHostingExtension.Value?.IsNativeElement(content) ?? false;

	internal static void AttachNativeElement(object owner, object content) => _nativeElementHostingExtension.Value?.AttachNativeElement(owner, content);

	internal static void DetachNativeElement(object owner, object content) => _nativeElementHostingExtension.Value?.DetachNativeElement(owner, content);

	internal static void ArrangeNativeElement(object owner, object content, Rect arrangeRect) => _nativeElementHostingExtension.Value?.ArrangeNativeElement(owner, content, arrangeRect);

	internal static Size MeasureNativeElement(object owner, object content, Size size) => _nativeElementHostingExtension.Value?.MeasureNativeElement(owner, content, size) ?? size;

	internal static bool IsNativeElementAttached(object owner, object nativeElement) => _nativeElementHostingExtension.Value?.IsNativeElementAttached(owner, nativeElement) ?? false;
}
