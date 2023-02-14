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

using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.Extensions;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.UI.Xaml.Input;
using Uno.UI.Xaml.Core;
using Uno.UI.DataBinding;
using Uno.UI.Xaml;

namespace Microsoft.UI.Xaml
{
	public partial class UIElement : DependencyObject
	{
		private ContainerVisual _visual;
		internal double _canvasTop;
		internal double _canvasLeft;
		private Rect _currentFinalRect;

		public UIElement()
		{
			_isFrameworkElement = this is FrameworkElement;

			Initialize();
			InitializePointers();
			InitializeKeyboard();

			this.RegisterPropertyChangedCallbackStrong(OnPropertyChanged);

			UpdateHitTest();
		}

		internal bool IsChildrenRenderOrderDirty { get; set; } = true;

		partial void InitializeKeyboard();

		private void OnPropertyChanged(ManagedWeakReference instance, DependencyProperty property, DependencyPropertyChangedEventArgs args)
		{
			if (property == Controls.Canvas.TopProperty)
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
			get
			{

				if (_visual == null)
				{
					_visual = Window.Current.Compositor.CreateContainerVisual();
					_visual.Comment = $"Owner:{GetType()}/{Name}";
				}

				return _visual;
			}
		}

		internal bool ClippingIsSetByCornerRadius { get; set; }

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
			Visual.IsChildrenRenderOrderDirty = true;

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
				Visual.IsChildrenRenderOrderDirty = true;
			}
			OnChildRemoved(child);
		}

		internal UIElement FindFirstChild() => _children.FirstOrDefault();

		#region Name Dependency Property

		private void OnNameChanged(string oldValue, string newValue)
		{
			if (FrameworkElementHelper.IsUiAutomationMappingEnabled)
			{
				Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(this, newValue);
			}
		}

		[GeneratedDependencyProperty(DefaultValue = "", ChangedCallback = true)]
		internal static DependencyProperty NameProperty { get; } = CreateNameProperty();

		public string Name
		{
			get => GetNameValue();
			set => SetNameValue(value);
		}

		#endregion

		partial void InitializeCapture();

		internal bool IsPointerCaptured { get; set; }

		public virtual IEnumerable<UIElement> GetChildren() => _children;

		public IntPtr Handle { get; set; }

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

			ApplyNativeClip(clip ?? Rect.Empty);
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

#if DEBUG
		public string ShowLocalVisualTree() => this.ShowLocalVisualTree(1000);
#endif
	}
}
