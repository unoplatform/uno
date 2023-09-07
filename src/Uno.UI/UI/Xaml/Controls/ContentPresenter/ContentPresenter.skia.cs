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

namespace Microsoft.UI.Xaml.Controls
{
	/// <summary>
	/// Declares a Content presenter
	/// </summary>
	/// <remarks>
	/// The content presenter is used for compatibility with WPF concepts,
	/// but the ContentSource property is not available, because there are ControlTemplates for now.
	/// </remarks>
	public partial class ContentPresenter : FrameworkElement
	{
		private static Lazy<INativeElementHostingExtension> _nativeElementHostingExtension = new Lazy<INativeElementHostingExtension>(() => ApiExtensibility.CreateInstance<INativeElementHostingExtension>(typeof(ContentPresenter)));

		private BorderLayerRenderer _borderRenderer = new BorderLayerRenderer();
		private Rect? _lastArrangeRect;
		private Rect _lastGlobalRect;
		private bool _nativeHostRegistered;

		public ContentPresenter()
		{
			InitializeContentPresenter();

			Loaded += (s, e) => RegisterNativeHostSupport();
			Unloaded += (s, e) => UnregisterNativeHostSupport();
			LayoutUpdated += (s, e) => UpdateBorder();
		}

		private void SetUpdateTemplate()
		{
			UpdateContentTemplateRoot();
		}

		partial void RegisterContentTemplateRoot()
		{
			AddChild(ContentTemplateRoot);
		}

		partial void UnregisterContentTemplateRoot()
		{
			RemoveChild(ContentTemplateRoot);
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

		private void UpdateCornerRadius(CornerRadius radius) => UpdateBorder();

		private void UpdateBorder()
		{
			if (IsLoaded)
			{
				_borderRenderer.UpdateLayer(
					this,
					Background,
					BackgroundSizing,
					BorderThickness,
					BorderBrush,
					CornerRadius,
					null
				);
			}
		}

		private void ClearBorder()
		{
			_borderRenderer.Clear();
		}

		partial void OnPaddingChangedPartial(Thickness oldValue, Thickness newValue)
		{
			UpdateBorder();
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

					_nativeElementHostingExtension.Value.ArrangeNativeElement(XamlRoot, Content, globalRect);
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
}
