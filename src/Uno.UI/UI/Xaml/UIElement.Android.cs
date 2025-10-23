using Uno.UI;
using Uno.UI.Controls;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using AndroidX.Core.View;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Android.Graphics;
using Android.Views;
using Matrix = Microsoft.UI.Xaml.Media.Matrix;
using Point = Windows.Foundation.Point;
using Rect = Windows.Foundation.Rect;
using Java.Interop;
using Microsoft.UI.Xaml.Markup;

namespace Microsoft.UI.Xaml
{
	public partial class UIElement : BindableView, Uno.UI.IUIElement
	{
		/// <summary>
		/// Keeps the count of native children (non-UIElements), for clipping purposes.
		/// </summary>
		private int _nativeChildrenCount;
		private bool _nativeClipChildren;

		private Rect _previousClip = Rect.Empty;

		private void ComputeAreChildrenNativeViewsOnly()
		{
			var nativeClipChildren = (this as IShadowChildrenProvider).ChildrenShadow.Count == _nativeChildrenCount;

			if (_nativeClipChildren != nativeClipChildren)
			{
				_nativeClipChildren = nativeClipChildren;

				// Non-UIElements typically expect to be clipped, and display incorrectly otherwise
				// This won't work when UIElements and non-UIElements are mixed in the same Panel,
				// but it should cover most cases in practice, and anyway should be superceded when
				// IFrameworkElement will be removed.
				SetClipChildren(FeatureConfiguration.UIElement.AlwaysClipNativeChildren ? _nativeClipChildren : false);
			}
		}

		protected override void OnLocalViewRemoved(View view)
		{
			base.OnLocalViewRemoved(view);

			if (view is not UIElement)
			{
				_nativeChildrenCount--;
			}

			ComputeAreChildrenNativeViewsOnly();
		}

		/// <inheritdoc />
		protected override void OnChildViewAdded(View view)
		{
			if (view is UIElement uiElement)
			{
				OnChildManagedViewAddedOrRemoved(uiElement);
			}
			else
			{
				_nativeChildrenCount++;
			}

			ComputeAreChildrenNativeViewsOnly();
		}

		/// <inheritdoc />
		protected override void OnChildViewRemoved(View view)
		{
			if (view is UIElement uiElement)
			{
				OnChildManagedViewAddedOrRemoved(uiElement);
			}
			else
			{
				_nativeChildrenCount--;
			}

			ComputeAreChildrenNativeViewsOnly();

			Shutdown();
		}

		private void OnChildManagedViewAddedOrRemoved(UIElement uiElement)
		{
			uiElement.ResetLayoutFlags();
			SetLayoutFlags(LayoutFlag.MeasureDirty);
			uiElement.SetLayoutFlags(LayoutFlag.MeasureDirty);
			uiElement.IsMeasureDirtyPathDisabled = IsMeasureDirtyPathDisabled;
		}

		public UIElement()
			: base(ContextHelper.Current)
		{
			Initialize();
			InitializePointers();
		}

		/// <summary>
		/// On Android, the equivalent of the "Dirty Path" is the native
		/// "Layout Requested" mechanism.
		/// </summary>
		internal bool IsMeasureDirtyPath
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => IsLayoutRequested;
		}

		/// <summary>
		/// Determines if InvalidateArrange has been called
		/// </summary>
		internal bool IsArrangeDirty => IsLayoutRequested;

		/// <summary>
		/// Not implemented yet on this platform.
		/// </summary>
		internal bool IsArrangeDirtyPath
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => false;
		}

		/// <summary>
		/// Gets the **logical** frame (a.k.a. 'finalRect') of the element while it's being arranged by a managed parent.
		/// </summary>
		/// <remarks>Used to keep "double" precision of arrange phase.</remarks>
		private protected Rect? TransientArrangeFinalRect { get; private set; }

		/// <summary>
		/// The difference between the physical layout width and height taking the origin into account,
		/// and the physical width and height that would've been calculated for an origin of (0,0).
		/// The difference may be -1,0, or +1 pixels due to different roundings.
		///
		/// (Eg, consider a Grid that is 31 logical pixels high, with 3 children with alignment Stretch in successive Star-sized rows.
		/// Each child will be measured with a logical height of 10.3, and logical origins of 0, 10.3, and 20.6.  Assume the device scale is 1.
		/// The child origins will be converted to 0, 10, and 21 respectively in integer pixel values; this will give heights of 10, 11, and 10 pixels.
		/// The FrameRoundingAdjustment values will be (0,0), (0,1), and (0,0) respectively.
		/// </summary>
		internal Size? FrameRoundingAdjustment { get; set; }

		internal void SetFramePriorArrange(Rect frame /* a.k.a 'finalRect' */, Rect physicalFrame)
		{
			var physicalWidth = ViewHelper.LogicalToPhysicalPixels(frame.Width);
			var physicalHeight = ViewHelper.LogicalToPhysicalPixels(frame.Height);

			TransientArrangeFinalRect = frame;
			FrameRoundingAdjustment = new Size(
				(int)physicalFrame.Width - physicalWidth,
				(int)physicalFrame.Height - physicalHeight);
		}

		internal void ResetFramePostArrange()
		{
			TransientArrangeFinalRect = null;
		}

		partial void ApplyNativeClip(Rect rect)
		{
			if (rect.IsEmpty)
			{
				if (_previousClip != rect)
				{
					_previousClip = rect;

#pragma warning disable CS0618 // deprecated members
					ViewCompat.SetClipBounds(this, null);
#pragma warning restore CS0618 // deprecated members
				}

				return;
			}

			_previousClip = rect;

			var physicalRect = rect.LogicalToPhysicalPixels();
			if (FrameRoundingAdjustment is { } fra)
			{
				physicalRect.Width += fra.Width;
				physicalRect.Height += fra.Height;
			}

#pragma warning disable CS0618 // deprecated members
			ViewCompat.SetClipBounds(this, physicalRect);
#pragma warning restore CS0618 // deprecated members

			if (FeatureConfiguration.UIElement.UseLegacyClipping)
			{
				// Old way: apply the clipping for each child on their assigned slot
				SetClipChildren(NeedsClipToSlot);
			}
			else
			{
				// "New" correct way: apply the clipping on the parent,
				// and let the children overflow inside the parent's bounds
				// This is closer to the XAML way of doing clipping.
				SetClipToPadding(NeedsClipToSlot);
			}
		}

		/// <summary>
		/// This method is called from the OnDraw of elements supporting rounded corners:
		/// Border, Rectangle, Panel...
		/// </summary>
		private protected void AdjustCornerRadius(ACanvas canvas, CornerRadius cornerRadius)
		{
			if (cornerRadius != CornerRadius.None)
			{
				UIElementNative.AdjustCornerRadius(canvas, cornerRadius.GetRadii());
			}
		}

		private bool _renderTransformRegisteredParentChanged;
		private static void RenderTransformOnParentChanged(object dependencyObject, object _, DependencyObjectParentChangedEventArgs args)
			=> ((UIElement)dependencyObject)._renderTransform?.UpdateParent(args.PreviousParent, args.NewParent);
		partial void OnRenderTransformSet()
		{
			// On first Transform set, we register to the parent changed, so we can enable or disable the static transformations on it
			if (!_renderTransformRegisteredParentChanged)
			{
				((IDependencyObjectStoreProvider)this).Store.RegisterSelfParentChangedCallback(RenderTransformOnParentChanged);
				_renderTransformRegisteredParentChanged = true;
			}
		}

		/// <summary>
		/// WARNING: This provides an approximation for Android.View only.
		/// Prefer to use the GetTransform between 2 UIElement when possible.
		/// </summary>
		internal static GeneralTransform TransformToVisual(View element, View visual)
		{
			var thisRect = new int[2];
			var otherRect = new int[2];
			element.GetLocationOnScreen(thisRect);

			// If visual is null, we transform the element to the window
			if (visual == null)
			{
				// Do nothing (leave at 0,0)
			}
			else
			{
				visual.GetLocationOnScreen(otherRect);
			}

			var x = thisRect[0] - otherRect[0];
			var y = thisRect[1] - otherRect[1];

			return new MatrixTransform
			{
				Matrix = new Matrix(
					m11: 1,
					m12: 0,
					m21: 0,
					m22: 1,
					offsetX: ViewHelper.PhysicalToLogicalPixels(x),
					offsetY: ViewHelper.PhysicalToLogicalPixels(y)
				)
			};
		}

		/// <summary>
		/// Note: Offsets are only an approximation which does not take in consideration possible transformations
		///	applied by a 'ViewGroup' between this element and its parent UIElement.
		/// </summary>
		private bool TryGetParentUIElementForTransformToVisual(out UIElement parentElement, ref Matrix3x2 matrix)
		{
			var parent = this.GetVisualTreeParent();
			switch (parent)
			{
				// First we try the direct parent, if it's from the known type we won't even have to adjust offsets

				case UIElement elt:
					parentElement = elt;
					return true;

				case null:
					parentElement = null;
					return false;

				case NativeListViewBase lv:
					// We are a container of a ListView, there is known issue with the LayoutingSlot.
					// Instead of following the standard transform computation, we shortcut few levels of the visual tree
					// and directly detect the real slot of this item in the parent ScrollViewer using native API.
					// The limitation with this is that if there is any RenderTransform in the bypassed layers which also
					// not only changes the TrX/Y, they are going to be ignored.
					// cf. https://github.com/unoplatform/uno/issues/2754

					// 1. Undo what was done by the shared code
					matrix.M31 -= (float)LayoutSlotWithMarginsAndAlignments.X;
					matrix.M32 -= (float)LayoutSlotWithMarginsAndAlignments.Y;

					// 2.Natively compute the offset of this current item relative to this ScrollViewer and adjust offsets
					var sv = lv.FindFirstParent<ScrollViewer>();
					var offset = GetPosition(this, relativeTo: sv);
					matrix.M31 += (float)offset.X;
					matrix.M32 += (float)offset.Y;

					// We return the parent of the ScrollViewer, so we bypass the <Horizontal|Vertical>Offset (and the Scale) handling in shared code.
					return sv.TryGetParentUIElementForTransformToVisual(out parentElement, ref matrix);

				case View view: // Android.View and Android.IViewParent
					var windowToFirstParent = new int[2];
					view.GetLocationInWindow(windowToFirstParent);

					do
					{
						parent = parent.GetVisualTreeParent();

						switch (parent)
						{
							case UIElement eltParent:
								// We found a UIElement in the parent hierarchy, we compute the X/Y offset between the
								// first parent 'view' and this 'elt', and return it.

								var windowToEltParent = new int[2];
								eltParent.GetLocationInWindow(windowToEltParent);

								parentElement = eltParent;
								matrix.M31 += (float)ViewHelper.PhysicalToLogicalPixels(windowToFirstParent[0] - windowToEltParent[0]);
								matrix.M32 += (float)ViewHelper.PhysicalToLogicalPixels(windowToFirstParent[1] - windowToEltParent[1]);
								return true;

							case null:
								// We reached the top of the window without any UIElement in the hierarchy,
								// so we adjust offsets using the X/Y position of the original 'view' in the window.

								parentElement = null;
								matrix.M31 += (float)ViewHelper.PhysicalToLogicalPixels(windowToFirstParent[0]);
								matrix.M32 += (float)ViewHelper.PhysicalToLogicalPixels(windowToFirstParent[1]);
								return false;
						}
					} while (true);
			}
		}

		partial void OnVisibilityChangedPartial(Visibility oldValue, Visibility newValue)
		{
			var newNativeVisibility = newValue == Visibility.Visible ? ViewStates.Visible : ViewStates.Gone;

			var bindableView = ((object)this) as Uno.UI.Controls.BindableView;

			if (bindableView != null)
			{
				// This cast is different for performance reasons. See the
				// UnoViewGroup java class for more details.
				bindableView.Visibility = newNativeVisibility;
				bindableView.RequestLayout();
			}
			else
			{
				((View)this).Visibility = newNativeVisibility;
				((View)this).RequestLayout();
			}
		}

		partial void OnOpacityChanged(DependencyPropertyChangedEventArgs args)
		{
			Alpha = IsRenderingSuspended ? 0 : (float)Opacity;
		}

		internal static Point GetPosition(View view, View relativeTo = null)
		{
			if (view == relativeTo)
			{
				return default;
			}

			var windowToThis = new int[2];
			view.GetLocationInWindow(windowToThis);

			var location = new Point(windowToThis[0], windowToThis[1]);

			if (relativeTo != null)
			{
				var windowToRelative = new int[2];
				relativeTo.GetLocationInWindow(windowToRelative);

				location.X -= windowToRelative[0];
				location.Y -= windowToRelative[1];
			}

			return location.PhysicalToLogicalPixels();
		}

		internal Point GetPosition(Point position, UIElement relativeTo)
		{
			if (relativeTo == this)
			{
				return position;
			}

			var currentViewLocation = new int[2];
			GetLocationInWindow(currentViewLocation);

			if (relativeTo == null)
			{
				return new Point(
					position.X + ViewHelper.PhysicalToLogicalPixels(currentViewLocation[0]),
					position.Y + ViewHelper.PhysicalToLogicalPixels(currentViewLocation[1])
				);
			}

			var relativeToLocation = new int[2];
			relativeTo.GetLocationInWindow(relativeToLocation);

			return new Point(
				position.X + ViewHelper.PhysicalToLogicalPixels(currentViewLocation[0] - relativeToLocation[0]),
				position.Y + ViewHelper.PhysicalToLogicalPixels(currentViewLocation[1] - relativeToLocation[1])
			);
		}


		/// <summary>
		/// Sets the specified dependency property value using the format "name|value"
		/// </summary>
		/// <param name="dependencyPropertyNameAndvalue">The name and value of the property</param>
		/// <returns>The currenty set value at the Local precedence</returns>
		public string SetDependencyPropertyValue(string dependencyPropertyNameAndValue)
			=> SetDependencyPropertyValueInternal(this, dependencyPropertyNameAndValue);

		string IUIElement.SetDependencyPropertyValue(string dependencyPropertyNameAndValue)
			=> SetDependencyPropertyValue(dependencyPropertyNameAndValue);

		/// <summary>
		/// Provides a native value for the dependency property with the given name on the current instance. If the value is a primitive type,
		/// its native representation is returned. Otherwise, the <see cref="object.ToString"/> implementation is used/returned instead.
		/// </summary>
		/// <param name="dependencyPropertyName">The name of the target dependency property</param>
		/// <returns>The content of the target dependency property (its actual value if it is a primitive type ot its <see cref="object.ToString"/> representation otherwise</returns>
		public Java.Lang.Object GetDependencyPropertyValue(string dependencyPropertyName)
		{
			var dpValue = GetDependencyPropertyValueInternal(this, dependencyPropertyName);
			if (dpValue == null)
			{
				return null;
			}

			var jObject = dpValue as Java.Lang.Object;
			if (jObject != null)
			{
				return jObject;
			}

#pragma warning disable CS0618 // deprecated members
#pragma warning disable CA1422 // Validate platform compatibility

			var type = dpValue.GetType();
			if (type == typeof(bool))
			{
				return new Java.Lang.Boolean((bool)dpValue);
			}
			else if (type == typeof(sbyte))
			{
				return new Java.Lang.Byte((sbyte)dpValue);
			}
			else if (type == typeof(char))
			{
				return new Java.Lang.Character((char)dpValue);
			}
			else if (type == typeof(short))
			{
				return new Java.Lang.Short((short)dpValue);
			}
			else if (type == typeof(int))
			{
				return new Java.Lang.Integer((int)dpValue);
			}
			else if (type == typeof(long))
			{
				return new Java.Lang.Long((long)dpValue);
			}
			else if (type == typeof(float))
			{
				return new Java.Lang.Float((float)dpValue);
			}
			else if (type == typeof(double))
			{
				return new Java.Lang.Double((double)dpValue);
			}
			else if (type == typeof(string))
			{
				return new Java.Lang.String((string)dpValue);
			}

			// If all else fails, just return the string representation of the DP's value
			return new Java.Lang.String(dpValue.ToString());
#pragma warning restore CA1422 // Validate platform compatibility
#pragma warning restore CS0618 // deprecated members
		}

		Java.Lang.Object IUIElement.GetDependencyPropertyValue(string dependencyPropertyName)
			=> GetDependencyPropertyValue(dependencyPropertyName);

#if DEBUG
		public static Predicate<View> ViewOfInterestSelector { get; set; } = v => (v as FrameworkElement)?.Name == "TargetView";

		public bool IsViewOfInterest => ViewOfInterestSelector(this);

		/// <summary>
		/// Returns the first view matching <see cref="ViewOfInterestSelector"/> anywhere in the visual tree. Handy when debugging Uno.
		/// </summary>
		/// <remarks>This property is intended as a shortcut to inspect the properties of a specific view at runtime. Suggested usage:
		/// 1. Be debugging Uno. 2. Flag the view you want in xaml with 'Name = "TargetView", or set <see cref="ViewOfInterestSelector"/>
		/// to select the view you want. 3. Put a breakpoint in the <see cref="UIElement.NativeHitCheck"/> method. 4. Tap anywhere in the app.
		/// 5. Inspect this property, or one of the typed versions below.</remarks>
		public View ViewOfInterest
		{
			get
			{
				ViewGroup topLevel = this;
				while (topLevel.Parent is ViewGroup newTopLevel)
				{
					topLevel = newTopLevel;
				}

				return GetMatchInChildren(topLevel);

				View GetMatchInChildren(ViewGroup parent)
				{
					if (parent == null)
					{
						return null;
					}

					for (int i = 0; i < parent.ChildCount; i++)
					{
						var child = parent.GetChildAt(i);
						if (ViewOfInterestSelector(child))
						{
							return child;
						}

						var inChild = GetMatchInChildren(child as ViewGroup);

						if (inChild != null)
						{
							return inChild;
						}
					}

					return null;
				}
			}
		}

		/// <summary>
		/// Convenience method to find all views with the given name.
		/// </summary>
		public FrameworkElement[] FindViewsByName(string name) => FindViewsByName(name, searchDescendantsOnly: false);


		/// <summary>
		/// Convenience method to find all views with the given name.
		/// </summary>
		/// <param name="searchDescendantsOnly">If true, only look in descendants of the current view; otherwise search the entire visual tree.</param>
		public FrameworkElement[] FindViewsByName(string name, bool searchDescendantsOnly)
		{

			View topLevel = this;

			if (!searchDescendantsOnly)
			{
				while (topLevel.Parent is View newTopLevel)
				{
					topLevel = newTopLevel;
				}
			}

			return GetMatchesInChildren(topLevel).ToArray();

			IEnumerable<FrameworkElement> GetMatchesInChildren(View parentView)
			{
				if (!(parentView is ViewGroup parent))
				{
					yield break;
				}

				foreach (var child in parent.GetChildren())
				{
					if (child is FrameworkElement fe && fe.Name == name)
					{
						yield return fe;
					}

					foreach (var match in GetMatchesInChildren(child))
					{
						yield return match;
					}
				}
			}
		}

		// Typed properties for easier inspection

		public Controls.ContentControl ContentControlOfInterest => ViewOfInterest as Controls.ContentControl;

		public Controls.Panel PanelOfInterest => ViewOfInterest as Controls.Panel;

		public FrameworkElement FrameworkElementOfInterest => ViewOfInterest as FrameworkElement;

		public string ShowDescendants() => Uno.UI.ViewExtensions.ShowDescendants(this);
		public string ShowLocalVisualTree(int fromHeight) => Uno.UI.ViewExtensions.ShowLocalVisualTree(this, fromHeight);
#endif
	}
}
