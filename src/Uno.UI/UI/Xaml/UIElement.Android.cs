using Uno.UI;
using Uno.UI.Controls;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml.Controls;
using Android.Support.V4.View;
using Windows.UI.Xaml.Media;
using Android.Graphics;
using Android.Views;
using Matrix = Windows.UI.Xaml.Media.Matrix;
using Point = Windows.Foundation.Point;
using Rect = Windows.Foundation.Rect;
using Java.Interop;
using Windows.UI.Xaml.Markup;

namespace Windows.UI.Xaml
{
	public partial class UIElement : BindableView
	{
		/// <summary>
		/// Returns true if this element has children and they are all native (non-UIElements), false if it has no children or if at
		/// least one is a UIElement.
		/// </summary>
		private bool AreChildrenNativeViewsOnly
		{
			get
			{
				var shadow = (this as IShadowChildrenProvider).ChildrenShadow;
				if (shadow.Count == 0)
				{
					return false;
				}

				foreach (var child in shadow)
				{
					if (child is UIElement)
					{
						return false;
					}
				}

				return true;
			}
		}

		public UIElement()
			: base(ContextHelper.Current)
		{
			InitializePointers();
		}

		partial void ApplyNativeClip(Rect rect)
		{
			// Non-UIElements typically expect to be clipped, and display incorrectly otherwise
			// This won't work when UIElements and non-UIElements are mixed in the same Panel,
			// but it should cover most cases in practice, and anyway should be superceded when
			// IFrameworkElement will be removed.
			SetClipChildren(FeatureConfiguration.UIElement.AlwaysClipNativeChildren ? AreChildrenNativeViewsOnly : false);

			if (rect.IsEmpty)
			{
				ViewCompat.SetClipBounds(this, null);
				return;
			}

			ViewCompat.SetClipBounds(this, rect.LogicalToPhysicalPixels());

			SetClipChildren(NeedsClipToSlot);
		}

		/// <summary>
		/// This method is called from the OnDraw of elements supporting rounded corners:
		/// Border, Rectangle, Panel...
		/// </summary>
		private protected void AdjustCornerRadius(Android.Graphics.Canvas canvas, CornerRadius cornerRadius)
		{
			if (cornerRadius != CornerRadius.None)
			{
				var rect = new RectF(canvas.ClipBounds);
				var clipPath = cornerRadius.GetOutlinePath(rect);
				canvas.ClipPath(clipPath);
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
		private bool TryGetParentUIElementForTransformToVisual(out UIElement parentElement, ref double offsetX, ref double offsetY)
		{
			var parent = this.GetParent();
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

					// 1. Undo what was done by the shared code
					offsetX -= LayoutSlotWithMarginsAndAlignments.X;
					offsetY -= LayoutSlotWithMarginsAndAlignments.Y;

					// 2.Natively compute the offset of this current item relative to this ScrollViewer and adjust offsets
					var sv = lv.FindFirstParent<ScrollViewer>();
					var offset = GetPosition(this, relativeTo: sv);
					offsetX += offset.X;
					offsetY += offset.Y;

					// We return the parent of the ScrollViewer, so we bypass the <Horizontal|Vertical>Offset (and the Scale) handling in shared code.
					return sv.TryGetParentUIElement(out parentElement, ref offsetX, ref offsetY);

				case View view: // Android.View and Android.IViewParent
					var windowToFirstParent = new int[2];
					view.GetLocationInWindow(windowToFirstParent);

					do
					{
						parent = parent.GetParent();

						switch (parent)
						{
							case UIElement eltParent:
								// We found a UIElement in the parent hierarchy, we compute the X/Y offset between the
								// first parent 'view' and this 'elt', and return it.

								var windowToEltParent = new int[2];
								eltParent.GetLocationInWindow(windowToEltParent);

								parentElement = eltParent;
								offsetX += ViewHelper.PhysicalToLogicalPixels(windowToFirstParent[0] - windowToEltParent[0]);
								offsetY += ViewHelper.PhysicalToLogicalPixels(windowToFirstParent[1] - windowToEltParent[1]);
								return true;

							case null:
								// We reached the top of the window without any UIElement in the hierarchy,
								// so we adjust offsets using the X/Y position of the original 'view' in the window.

								parentElement = null;
								offsetX += ViewHelper.PhysicalToLogicalPixels(windowToFirstParent[0]);
								offsetY += ViewHelper.PhysicalToLogicalPixels(windowToFirstParent[1]);
								return false;
						}
					} while (true);

				default:
					Application.Current.RaiseRecoverableUnhandledException(new InvalidOperationException("Found a parent which is NOT a View."));

					parentElement = null;
					return false;
			}
		}

		protected virtual void OnVisibilityChanged(Visibility oldValue, Visibility newValue)
		{
			var newNativeVisibility = newValue == Visibility.Visible ? Android.Views.ViewStates.Visible : Android.Views.ViewStates.Gone;

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
		[Java.Interop.Export(nameof(SetDependencyPropertyValue))]
		public string SetDependencyPropertyValue(string dependencyPropertyNameAndValue)
			=> SetDependencyPropertyValueInternal(this, dependencyPropertyNameAndValue);

		/// <summary>
		/// Provides a native value for the dependency property with the given name on the current instance. If the value is a primitive type, 
		/// its native representation is returned. Otherwise, the <see cref="object.ToString"/> implementation is used/returned instead.
		/// </summary>
		/// <param name="dependencyPropertyName">The name of the target dependency property</param>
		/// <returns>The content of the target dependency property (its actual value if it is a primitive type ot its <see cref="object.ToString"/> representation otherwise</returns>
		[Java.Interop.Export(nameof(GetDependencyPropertyValue))]
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
		}

		internal Rect? ArrangeLogicalSize { get; set; } // Used to keep "double" precision of arrange phase

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

		public string ShowDescendants() => ViewExtensions.ShowDescendants(this);
		public string ShowLocalVisualTree(int fromHeight) => ViewExtensions.ShowLocalVisualTree(this, fromHeight);
#endif
	}
}
