#if DEBUG
#define ENABLE_CONTAINER_VISUAL_TRACKING
#endif

using Windows.Foundation;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Linq;
using Windows.UI.Composition;
using System.Numerics;
using Windows.Foundation.Metadata;

using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.Extensions;
using Windows.UI.Xaml.Controls.Primitives;
using Uno.UI.Xaml.Input;
using Uno.UI.Xaml.Core;
using Uno.UI.DataBinding;
using Uno.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Uno.UI.Media;
using Uno.UI.Dispatching;
using Uno.Collections;
using Uno.UI.Xaml.Controls;

namespace Windows.UI.Xaml
{
	public partial class UIElement : DependencyObject, IVisualElement, IVisualElement2
	{
		private protected ShapeVisual _visual;
		private Rect _lastFinalRect;
		private Rect? _lastClippedFrame;

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
				new FrameworkPropertyMetadata(true, propertyChangedCallback: (o, _) => (o as IBorderInfoProvider)?.UpdateBorderThickness()));

		partial void OnOpacityChanged(DependencyPropertyChangedEventArgs args)
		{
			UpdateOpacity();
			ContentPresenter.UpdateNativeHostContentPresentersOpacities();
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
					_visual = CreateElementVisual();
					Debug.Assert(this is not IBorderInfoProvider || _visual is BorderVisual,
						"Border info providers are expected to override CreateElementVisual and return BorderVisual, and types returning BorderVisual should be IBorderInfoProviders");
#if ENABLE_CONTAINER_VISUAL_TRACKING
					_visual.Comment = $"{this.GetDebugDepth():D2}-{this.GetDebugName()}";
#endif
				}

				return _visual;
			}
		}

		private protected virtual ShapeVisual CreateElementVisual() => Compositor.GetSharedCompositor().CreateShapeVisual();

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

			var enterParams = new EnterParams(IsActiveInVisualTree);
			ChildEnter(child, enterParams);

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
			var view = _children[oldIndex];

			_children.RemoveAt(oldIndex);
			if (newIndex == _children.Count)
			{
				_children.Add(view);
			}
			else
			{
				_children.Insert(newIndex, view);
			}

			InvalidateMeasure();
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
			if (_children.Count == 0)
			{
				return;
			}

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

			var oldFinalRect = _lastFinalRect;
			var oldClippedFrame = _lastClippedFrame;
			_lastFinalRect = finalRect;
			_lastClippedFrame = clippedFrame;

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

				OnArrangeVisual(newRect, clippedFrame);
				OnViewportUpdated();
			}
			else
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"{this}: ArrangeVisual({_lastFinalRect}) -- SKIPPED (no change)");
				}
			}
		}

		internal virtual void OnArrangeVisual(Rect rect, Rect? clip)
		{
			// Note: rect has already been rounded, if needed, during arrange.
			var visual = Visual;
			visual.Offset = new Vector3((float)rect.X, (float)rect.Y, 0) + _translation;
			visual.Size = new Vector2((float)rect.Width, (float)rect.Height);
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
				viewBox.IsAncestorClip = ShouldApplyLayoutClipAsAncestorClip();

				viewBox.Offset = clip.Value.Location.ToVector2();
				viewBox.Size = clip.Value.Size.ToVector2();

				visual.ViewBox = viewBox;
			}
		}

		partial void ApplyNativeClip(Rect rect, Transform transform)
		{
			if (rect.IsEmpty)
			{
				Visual.Clip = null;
			}
			else
			{
				var roundedRectClip = rect;
				if (GetUseLayoutRounding())
				{
					roundedRectClip = LayoutRound(roundedRectClip);
				}

				var compositionClip = Visual.Compositor.CreateRectangleClip(
					top: (float)roundedRectClip.Top,
					left: (float)roundedRectClip.Left,
					bottom: (float)roundedRectClip.Bottom,
					right: (float)roundedRectClip.Right
				);

				if (transform is { } clipTransform)
				{
					compositionClip.TransformMatrix = clipTransform.MatrixCore;
				}

				Visual.Clip = compositionClip;
			}
		}

		partial void ShowVisual()
			=> Visual.IsVisible = true;

		partial void HideVisual()
			=> Visual.IsVisible = false;

		public void StartAnimation(ICompositionAnimationBase animation)
		{
			if (animation is ExpressionAnimation expressionAnimation)
			{
				if (expressionAnimation.Target.Equals("Translation", StringComparison.OrdinalIgnoreCase) ||
					expressionAnimation.Target.StartsWith("Translation.", StringComparison.OrdinalIgnoreCase))
				{
					ElementCompositionPreview.SetIsTranslationEnabled(this, true);
				}

				Visual.StartAnimation(expressionAnimation.Target, expressionAnimation);
			}
			else
			{
				throw new NotSupportedException("The method 'UIElement.StartAnimation' currently only supports 'ExpressionAnimation'.");
			}
		}

		public void StopAnimation(ICompositionAnimationBase animation)
		{
			if (animation is ExpressionAnimation expressionAnimation)
			{
				Visual.StopAnimation(expressionAnimation.Target);
			}
			else
			{
				throw new NotSupportedException("The method 'UIElement.StartAnimation' currently only supports 'ExpressionAnimation'.");
			}
		}

		Visual IVisualElement2.GetVisualInternal() => ElementCompositionPreview.GetElementVisual(this);

#if DEBUG
		public string ShowLocalVisualTree() => this.ShowLocalVisualTree(1000);
#endif
	}
}
