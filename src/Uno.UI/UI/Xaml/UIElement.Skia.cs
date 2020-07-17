using Windows.Foundation;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Windows.UI.Composition;
using System.Numerics;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI;
using Uno.UI.Extensions;

namespace Windows.UI.Xaml
{
	public partial class UIElement : DependencyObject
	{
		// Even if this a concept of FrameworkElement, the loaded state is handled by the UIElement in order to avoid
		// to cast to FrameworkElement each time a child is added or removed.
		internal bool IsLoaded;

		internal Size _unclippedDesiredSize;
		internal Point _visualOffset;
		internal List<UIElement> _children = new List<UIElement>();
		private ContainerVisual _visual;
		private Visibility _visibilityCache;
		internal double _canvasTop;
		internal double _canvasLeft;
		private Rect _currentFinalRect;

		private protected int? Depth { get; private set; }

		public UIElement()
		{
			Initialize();
			InitializePointers();

			RegisterPropertyChangedCallback(VisibilityProperty, OnVisibilityPropertyChanged);
			RegisterPropertyChangedCallback(Controls.Canvas.LeftProperty, OnCanvasLeftChanged);
			RegisterPropertyChangedCallback(Controls.Canvas.TopProperty, OnCanvasTopChanged);

			UpdateHitTest();
		}

		private void OnCanvasTopChanged(DependencyObject sender, DependencyProperty dp)
		{
			_canvasTop = (double)this.GetValue(Controls.Canvas.TopProperty);
		}

		private void OnCanvasLeftChanged(DependencyObject sender, DependencyProperty dp)
		{
			_canvasLeft = (double)this.GetValue(Controls.Canvas.LeftProperty);
		}

		private void OnVisibilityPropertyChanged(DependencyObject sender, DependencyProperty dp)
		{
			UpdateHitTest();

			_visibilityCache = (Visibility)GetValue(VisibilityProperty);
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

			_children.Add(child);

			UpdateChildVisual(child);
			OnChildAdded(child);

			InvalidateMeasure();
		}

		private void OnChildAdded(UIElement child)
		{

		}

		internal virtual void OnElementLoaded()
		{
			IsLoaded = true;
			foreach (var innerChild in _children)
			{
				innerChild.OnElementLoaded();
			}
		}

		private bool IsParentLoaded()
		{
			var root = Window.Current.Content;

			var current = this.GetParent();

			while (current != null && current != root)
			{
				current = this.GetParent();
			}

			return current == root;
		}

		private void OnAddingChild(UIElement child)
		{
			if (IsLoaded)
			{
				child.OnElementLoaded();
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

		private void UpdateOpacity() => Visual.Opacity = Visibility == Visibility.Visible ? (float)Opacity : 0;

		private void UpdateChildVisual(UIElement child)
		{
			Visual.Children.InsertAtTop(child.Visual);
		}

		internal UIElement RemoveChild(UIElement child)
		{
			_children.Remove(child);
			child.SetParent(null);

			if (Visual != null)
			{
				Visual.Children.Remove(child.Visual);
			}

			return child;
		}

		internal void ClearChildren()
		{
			foreach (var child in _children)
			{
				child.SetParent(null);
				// OnChildRemoved(child);
			}

			_children.Clear();
			InvalidateMeasure();
		}

		internal UIElement FindFirstChild() => _children.FirstOrDefault();

		public string Name { get; set; }

		partial void InitializeCapture();

		internal bool IsPointerCaptured { get; set; }

		internal virtual bool IsEnabledOverride() => true;

		public virtual IEnumerable<UIElement> GetChildren() => _children;

		public IntPtr Handle { get; set; }

		internal Windows.Foundation.Point GetPosition(Point position, global::Windows.UI.Xaml.UIElement relativeTo)
		{
			throw new NotSupportedException();
		}

		protected virtual void OnVisibilityChanged(Visibility oldValue, Visibility newVisibility)
		{
			UpdateOpacity();

			if (newVisibility == Visibility.Collapsed)
			{
				_desiredSize = new Size(0, 0);
				_size = new Size(0, 0);
			}
		}

		partial void OnRenderTransformSet()
		{
		}

		internal void ArrangeVisual(Rect finalRect, bool clipToBounds, Rect? clippedFrame = default)
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

				Rect? getClip()
				{
					// Disable clipping for Scrollviewer (edge seems to disable scrolling if 
					// the clipping is enabled to the size of the scrollviewer, even if overflow-y is auto)
					if (this is Controls.ScrollViewer)
					{
						return null;
					}
					else if (Clip != null)
					{
						return Clip.Rect;
					}

					return new Rect(0, 0, newRect.Width, newRect.Height);
				}

				OnArrangeVisual(newRect, getClip());
			}
			else
			{
				this.Log().DebugIfEnabled(() => $"{this}: ArrangeVisual({_currentFinalRect}) -- SKIPPED (no change)");
			}
		}

		internal virtual void OnArrangeVisual(Rect rect, Rect? clip)
		{
			Visual.Offset = new Vector3((float)rect.X, (float)rect.Y, 0);
			Visual.Size = new Vector2((float)rect.Width, (float)rect.Height);

			if (clip is Rect rectClip)
			{
				Visual.Clip = new InsetClip() {
					TopInset = (float)rectClip.Top,
					LeftInset = (float)rectClip.Left,
					BottomInset = (float)rectClip.Bottom,
					RightInset = (float)rectClip.Right,
				};
			}
			else
			{
				Visual.Clip = null;
			}
		}
	}
}
