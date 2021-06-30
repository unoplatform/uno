using Windows.Foundation;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Windows.UI.Composition;
using System.Numerics;
using Windows.Foundation.Metadata;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI;
using Uno.UI.Extensions;
using Windows.UI.Xaml.Controls.Primitives;
using Uno.UI.DataBinding;

namespace Windows.UI.Xaml
{
	public partial class UIElement : DependencyObject
	{
		internal Size _unclippedDesiredSize;
		internal Point _visualOffset;
		private ContainerVisual _visual;
		internal double _canvasTop;
		internal double _canvasLeft;
		private Rect _currentFinalRect;

		public UIElement()
		{
			_log = this.Log();
			_logDebug = _log.IsEnabled(LogLevel.Debug) ? _log : null;
			_isFrameworkElement = this is FrameworkElement;

			Initialize();
			InitializePointers();
			InitializeKeyboard();

			this.RegisterPropertyChangedCallbackStrong(OnPropertyChanged);

			UpdateHitTest();
		}

		partial void InitializeKeyboard();

		private void OnPropertyChanged(ManagedWeakReference instance, DependencyProperty property, DependencyPropertyChangedEventArgs args)
		{
			if(property == Controls.Canvas.TopProperty)
			{
				_canvasTop = (double)args.NewValue;
			}
			else if (property == Controls.Canvas.LeftProperty)
			{
				_canvasLeft = (double)args.NewValue;
			}
		}


		partial void OnOpacityChanged(DependencyPropertyChangedEventArgs args)
		{
			UpdateOpacity();
		}

		partial void OnIsHitTestVisibleChangedPartial(bool oldValue, bool newValue)
		{
			UpdateHitTest();
		}

		private void UpdateOpacity()
		{
			Visual.Opacity = Visibility == Visibility.Visible ? (float)Opacity : 0;
		}

		internal ContainerVisual Visual
		{
			get {

				if (_visual == null)
				{
					_visual = Window.Current.Compositor.CreateContainerVisual();
					_visual.Comment = $"Owner:{GetType()}/{Name}";
				}

				return _visual;
			}
		} 

		/// <summary>
		/// The origin of the view's bounds relative to its parent.
		/// </summary>
		internal Point RelativePosition => _visualOffset;

		internal bool ClippingIsSetByCornerRadius { get; set; } = false;

		public void AddChild(UIElement child, int? index = null)
		{
			if (child == null)
			{
				return;
			}

			var currentParent = child.GetParent() as UIElement;

			// Remove child from current parent, if any
			if (currentParent != this && currentParent != null)
			{
				// ---IMPORTANT---
				// This behavior is different than UWP:
				// On UWP the behavior would be to throw an "Element already has a logical parent" exception.

				// It is done here to align Wasm with Android and iOS where the control is
				// simply "moved" when attached to another parent.

				// This could lead to "child kidnapping", like the one happening in ComboBox & ComboBoxItem

				this.Log().Info($"{this}.AddChild({child}): Removing child {child} from its current parent {currentParent}.");
				currentParent.RemoveChild(child);
			}

			child.SetParent(this);
			OnAddingChild(child);

			if (index is int actualIndex && actualIndex != _children.Count)
			{
				var currentVisual = _children[actualIndex];
				_children.Insert(actualIndex, child);
				Visual.Children.InsertAbove(child.Visual, currentVisual.Visual);
			}
			else
			{
				_children.Add(child);
				Visual.Children.InsertAtTop(child.Visual);
			}

			OnChildAdded(child);

			InvalidateMeasure();
		}

		internal void MoveChildTo(int oldIndex, int newIndex)
		{
			ApiInformation.TryRaiseNotImplemented("UIElement", "MoveChildTo");
		}

		internal bool RemoveChild(UIElement child)
		{
			if (_children.Remove(child))
			{
				InnerRemoveChild(child);
				return true;
			}
			else
			{
				return false;
			}
		}

		internal void ClearChildren()
		{
			foreach (var child in _children.ToArray())
			{
				InnerRemoveChild(child);
			}

			_children.Clear();
			InvalidateMeasure();
		}

		private void InnerRemoveChild(UIElement child)
		{
			child.SetParent(null);
			Visual?.Children.Remove(child.Visual);
			OnChildRemoved(child);
		}

		internal UIElement FindFirstChild() => _children.FirstOrDefault();

		public string Name { get; set; }

		partial void InitializeCapture();

		internal bool IsPointerCaptured { get; set; }

		public virtual IEnumerable<UIElement> GetChildren() => _children;

		public IntPtr Handle { get; set; }

		protected virtual void OnVisibilityChanged(Visibility oldValue, Visibility newVisibility)
		{
			UpdateHitTest();
			UpdateOpacity();

			if (newVisibility == Visibility.Collapsed)
			{
				LayoutInformation.SetDesiredSize(this, new Size(0, 0));
				_size = new Size(0, 0);
			}
		}

		partial void OnRenderTransformSet()
		{
		}

		internal void ArrangeVisual(Rect finalRect, Rect? clippedFrame = default)
		{
			LayoutSlotWithMarginsAndAlignments =
				VisualTreeHelper.GetParent(this) is UIElement parent
					? finalRect.DeflateBy(parent.GetBorderThickness())
					: finalRect;

			var oldFinalRect = _currentFinalRect;
			_currentFinalRect = finalRect;

			var oldRect = oldFinalRect;
			var newRect = finalRect;
			if (oldRect != newRect)
			{
				if (
					newRect.Width < 0
					|| newRect.Height < 0
					|| double.IsNaN(newRect.Width)
					|| double.IsNaN(newRect.Height)
					|| double.IsNaN(newRect.X)
					|| double.IsNaN(newRect.Y)
				)
				{
					throw new InvalidOperationException($"{this}: Invalid frame size {newRect}. No dimension should be NaN or negative value.");
				}

				Rect? clip;
				if (this is Controls.ScrollViewer)
				{
					clip = (Rect?)null;
				}
				else
				{
					clip = clippedFrame;
				}

				OnArrangeVisual(newRect, clip);
				OnViewportUpdated(clippedFrame ?? Rect.Empty);
			}
			else
			{
				this.Log().DebugIfEnabled(() => $"{this}: ArrangeVisual({_currentFinalRect}) -- SKIPPED (no change)");
			}
		}

		internal virtual void OnArrangeVisual(Rect rect, Rect? clip)
		{
			var roundedRect = LayoutRound(rect);

			Visual.Offset = new Vector3((float)roundedRect.X, (float)roundedRect.Y, 0);
			Visual.Size = new Vector2((float)roundedRect.Width, (float)roundedRect.Height);
			Visual.CenterPoint = new Vector3((float)RenderTransformOrigin.X, (float)RenderTransformOrigin.Y, 0);

			ApplyNativeClip(clip ?? Rect.Empty);
		}

		partial void ApplyNativeClip(Rect clip)
		{
			if (ClippingIsSetByCornerRadius)
			{
				return; // already applied
			}

			if (clip.IsEmpty)
			{
				Visual.Clip = null;
			}
			else
			{
				var roundedRectClip = LayoutRound(clip);

				Visual.Clip = Visual.Compositor.CreateInsetClip(
					topInset: (float)roundedRectClip.Top,
					leftInset: (float)roundedRectClip.Left,
					bottomInset: (float)roundedRectClip.Bottom,
					rightInset: (float)roundedRectClip.Right
				);
			}
		}

		partial void ShowVisual()
			=> Visual.IsVisible = true;

		partial void HideVisual()
			=> Visual.IsVisible = false;

#if DEBUG
		public string ShowLocalVisualTree() => this.ShowLocalVisualTree(1000);
#endif
	}
}
