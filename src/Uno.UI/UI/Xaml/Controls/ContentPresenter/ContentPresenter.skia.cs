using System;
using System.Runtime.InteropServices;
using Uno.Extensions;
using Uno.UI;
using Uno.UI.DataBinding;
using Windows.Foundation;
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
			if (CoreWindow.Main.IsNativeElement(newValue))
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
				CoreWindow.Main.AttachNativeElement(XamlRoot, Content);
			}
		}

		partial void TryDetachNativeElement()
		{
			if (IsNativeHost)
			{
				CoreWindow.Main.DetachNativeElement(XamlRoot, Content);
			}
		}

		private Size MeasureNativeElement(Size size)
		{
			if (IsNativeHost)
			{
				return CoreWindow.Main.MeasureNativeElement(XamlRoot, Content, size);
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

					CoreWindow.Main.ArrangeNativeElement(XamlRoot, Content, globalRect);
				}
			}
		}
	}
}
