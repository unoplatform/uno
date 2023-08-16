#if DEBUG
#define ENABLE_CONTAINER_VISUAL_TRACKING
#endif

using Windows.Foundation;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.UI.Composition;
using System.Numerics;
using Windows.Foundation.Metadata;

using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.Extensions;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.UI.Xaml.Input;
using Uno.UI.Xaml.Core;
using Uno.UI.DataBinding;
using Uno.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Uno.UI.Media;
using Uno.UI.Dispatching;
using Uno.Collections;

namespace Microsoft.UI.Xaml
{
	public partial class UIElement : DependencyObject, IVisualElement, IVisualElement2
	{
		private ShapeVisual _visual;
		private Rect _currentFinalRect;
		private Rect? _currentClippedFrame;

		public UIElement()
		{
			_isFrameworkElement = this is FrameworkElement;

			Initialize();
			InitializePointers();

			UpdateHitTest();
		}

		public bool UseLayoutRounding
		{
			get => (bool)this.GetValue(UseLayoutRoundingProperty);
			set => this.SetValue(UseLayoutRoundingProperty, value);
		}

		public static DependencyProperty UseLayoutRoundingProperty { get; } =
			DependencyProperty.Register(
				nameof(UseLayoutRounding),
				typeof(bool),
				typeof(UIElement),
				new FrameworkPropertyMetadata(true));

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

		internal ShapeVisual Visual
		{
			get
			{

				if (_visual is null)
				{
					_visual = Compositor.GetSharedCompositor().CreateShapeVisual();
#if ENABLE_CONTAINER_VISUAL_TRACKING
					_visual.Comment = $"{this.GetDebugDepth():D2}-{this.GetDebugName()}";
#endif
				}

				return _visual;
			}
		}

#if ENABLE_CONTAINER_VISUAL_TRACKING // Make sure to update the Comment to have the valid depth
		partial void OnLoading()
		{
			if (_visual is not null)
			{
				_visual.Comment = $"{this.GetDebugDepth():D2}-{this.GetDebugName()}";
			}
		}
#endif

		internal bool ClippingIsSetByCornerRadius { get; set; }

		internal void AddChild(UIElement child, int? index = null)
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

			if (index is { } actualIndex && actualIndex != _children.Count)
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

			// Reset to original (invalidated) state
			child.ResetLayoutFlags();

			if (IsMeasureDirtyPathDisabled)
			{
				FrameworkElementHelper.SetUseMeasurePathDisabled(child); // will invalidate too
			}
			else
			{
				child.InvalidateMeasure();
			}

			if (IsArrangeDirtyPathDisabled)
			{
				FrameworkElementHelper.SetUseArrangePathDisabled(child); // will invalidate too
			}
			else
			{
				child.InvalidateArrange();
			}

			// Force a new measure of this element (the parent of the new child)
			InvalidateMeasure();
			InvalidateArrange();

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

				// Force a new measure of this element
				InvalidateMeasure();

				return true;
			}

			return false;
		}

		internal UIElement ReplaceChild(int index, UIElement child)
		{
			var previous = _children[index];

			if (!ReferenceEquals(child, previous))
			{
				RemoveChild(previous);
				AddChild(child, index);
			}

			return previous;
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
			if (Visual != null)
			{
				Visual.Children.Remove(child.Visual);
			}
			OnChildRemoved(child);
		}

		internal UIElement FindFirstChild() => _children.FirstOrDefault();

		internal bool IsPointerCaptured { get; set; }

		internal MaterializableList<UIElement> GetChildren() => _children;

		public IntPtr Handle { get; }

		partial void OnVisibilityChangedPartial(Visibility oldValue, Visibility newValue)
		{
			UpdateHitTest();
			UpdateOpacity();

			if (newValue == Visibility.Collapsed)
			{
				LayoutInformation.SetDesiredSize(this, new Size(0, 0));
				_size = new Size(0, 0);
			}

			if (FeatureConfiguration.UIElement.UseInvalidateMeasurePath && this.GetParent() is UIElement parent)
			{
				// Need to invalidate the parent when the visibility changes to ensure its
				// algorithm is doing its layout properly.
				parent.InvalidateMeasure();
			}
		}

		partial void OnRenderTransformSet()
		{
		}

		internal void ArrangeVisual(Rect finalRect, Rect? clippedFrame = default)
		{
			LayoutSlotWithMarginsAndAlignments = finalRect;

			var oldFinalRect = _currentFinalRect;
			var oldClippedFrame = _currentClippedFrame;
			_currentFinalRect = finalRect;
			_currentClippedFrame = clippedFrame;

			var oldRect = oldFinalRect;
			var newRect = finalRect;

			var oldClip = oldClippedFrame;
			var newClip = clippedFrame;

			if (oldRect != newRect || oldClip != newClip || (_renderTransform?.FlowDirectionTransform ?? Matrix3x2.Identity) != GetFlowDirectionTransform())
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
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"{this}: ArrangeVisual({_currentFinalRect}) -- SKIPPED (no change)");
				}
			}
		}

		internal virtual void OnArrangeVisual(Rect rect, Rect? clip)
		{
			var roundedRect = LayoutRound(rect);

			var visual = Visual;
			visual.Offset = new Vector3((float)roundedRect.X, (float)roundedRect.Y, 0) + _translation;
			visual.Size = new Vector2((float)roundedRect.Width, (float)roundedRect.Height);
			visual.CenterPoint = new Vector3((float)RenderTransformOrigin.X, (float)RenderTransformOrigin.Y, 0);
			if (_renderTransform is null && !GetFlowDirectionTransform().IsIdentity)
			{
				_renderTransform = new NativeRenderTransformAdapter(this, RenderTransform, RenderTransformOrigin);
			}

			_renderTransform?.UpdateFlowDirectionTransform();

			// The clipping applied by our parent due to layout constraints are pushed to the visual through the ViewBox property
			// This allows special handling of this clipping by the compositor (cf. ShapeVisual.Render).
			if (clip is null)
			{
				visual.ViewBox = null;
			}
			else
			{
				var viewBox = visual.Compositor.CreateViewBox();
				viewBox.Offset = clip.Value.Location.ToVector2();
				viewBox.Size = clip.Value.Size.ToVector2();

				visual.ViewBox = viewBox;
			}
		}

		partial void ApplyNativeClip(Rect rect)
		{
			if (ClippingIsSetByCornerRadius)
			{
				return; // already applied
			}

			if (rect.IsEmpty)
			{
				Visual.Clip = null;
			}
			else
			{
				var roundedRectClip = LayoutRound(rect);

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

		Visual IVisualElement2.GetVisualInternal() => ElementCompositionPreview.GetElementVisual(this);

#if DEBUG
		public string ShowLocalVisualTree() => this.ShowLocalVisualTree(1000);
#endif
	}
}
