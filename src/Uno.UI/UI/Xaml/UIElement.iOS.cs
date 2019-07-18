using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using Microsoft.Extensions.Logging;
using UIKit;
using Uno.Extensions;
using Uno.UI;
using Uno.UI.Controls;
using Uno.UI.Extensions;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Input;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using UIViewExtensions = UIKit.UIViewExtensions;

namespace Windows.UI.Xaml
{
	public partial class UIElement : BindableUIView
	{

#if DEBUG
		/// <summary>
		/// Provides the ability to disable clipping for an object provided by the selector.
		/// </summary>
		public static Func<object, bool> CanClipSelector { get; set; }
#endif

		private static Dictionary<UIView, CALayer> _debugLayers;

		public UIElement()
		{
			InitializePointers();
			InitializeCapture();
		}

		partial void InitializeCapture();

		partial void EnsureClip(Rect rect)
		{
			if (rect.IsEmpty
				|| double.IsPositiveInfinity(rect.X)
				|| double.IsPositiveInfinity(rect.Y)
				|| double.IsPositiveInfinity(rect.Width)
				|| double.IsPositiveInfinity(rect.Height)
			)
			{
				this.Layer.Mask = null;
				return;
			}
			this.Layer.Mask = new CAShapeLayer
			{
				Path = CGPath.FromRect(ToCGRect(rect))
			};
		}

		partial void OnOpacityChanged(DependencyPropertyChangedEventArgs args)
		{
			// Don't update the internal value if the value is being animated.
			// The value is being animated by the platform itself.
			if (!(args.NewPrecedence == DependencyPropertyValuePrecedences.Animations && args.BypassesPropagation))
			{
				Alpha = IsRenderingSuspended ? 0 : (nfloat)Opacity;
			}
		}

		protected virtual void OnVisibilityChanged(Visibility oldValue, Visibility newValue)
		{
			var newVisibility = (Visibility)newValue;

			if (base.Hidden != newVisibility.IsHidden())
			{
				base.Hidden = newVisibility.IsHidden();
				this.SetNeedsLayout();

				if (newVisibility == Visibility.Visible)
				{
					// This recursively invalidates the layout of all subviews
					// to ensure LayoutSubviews is called and views get updated.
					// Failing to do this can cause some views to remain collapsed.
					SetSubviewsNeedLayout();
				}
			}
		}

		public override bool Hidden
		{
			get
			{
				return base.Hidden;
			}
			set
			{
				// Only set the Visility property, the Hidden property is updated 
				// in the property changed handler as there are actions associated with 
				// the change.
				Visibility = value ? Visibility.Collapsed : Visibility.Visible;
			}
		}

		public void SetSubviewsNeedLayout()
		{
			base.SetNeedsLayout();

			if (this is Controls.Panel p)
			{
				// This section is here because of the enumerator type returned by Children,
				// to avoid allocating during the enumeration.
				foreach (var view in p.Children)
				{
					(view as IFrameworkElement)?.SetSubviewsNeedLayout();
				}
			}
			else
			{
				foreach (var view in this.GetChildren())
				{
					(view as IFrameworkElement)?.SetSubviewsNeedLayout();
				}
			}
		}

		internal Windows.Foundation.Point GetPosition(Point position, global::Windows.UI.Xaml.UIElement relativeTo)
		{
			return relativeTo.ConvertPointToCoordinateSpace(position, relativeTo);
		}

		private CGRect ToCGRect(Rect rect)
		{
			return new CGRect
			(
				(nfloat)(rect.X),
				(nfloat)(rect.Y),
				(nfloat)(rect.Width),
				(nfloat)(rect.Height)
			);
		}

		public GeneralTransform TransformToVisual(UIElement visual)
		{
			// If visual is null, we transform the element to the window
			if (visual == null)
			{
				visual = Xaml.Window.Current.Content;
			}

			var unit = new CGRect(0, 0, 1, 1);
			var transformed = visual.ConvertRectFromView(unit, this);

			return new MatrixTransform
			{
				Matrix = new Matrix(
					m11: 1,
					m12: 0,
					m21: 0,
					m22: 1,
					offsetX: transformed.X,
					offsetY: transformed.Y
				)
			};
		}


		/// <summary>
		/// Gets the parent view for the <paramref name="owner"/> which clips its content.
		/// </summary>
		/// <returns>A tuple of the clipping parent, and the view that let to this parent.</returns>
		private static (UIView child, UIView clippingParent) GetClippingParent(UIView owner)
		{
			(UIView child, UIView clippingParent) GetClippingParent(UIView child, UIView parent)
			{
				if (parent is FrameworkElement pfe)
				{
					if (!pfe.ClipChildrenToBounds)
					{
						return GetClippingParent(pfe, pfe.Superview);
					}
					else
					{
						return (child, parent);
					}
				}
				else
				{
					return (child, parent);
				}
			}


			if (owner.Superview is FrameworkElement sfe && !sfe.ClipChildrenToBounds)
			{
				return GetClippingParent(owner, owner.Superview);
			}

			return (owner, owner.Superview);
		}

		internal static FrameworkElement UpdateMask(UIView owner, UIView superView = null)
		{
			superView = superView ?? owner.Superview;
			var layer = owner.Layer;

			var clippingParentResult = GetClippingParent(owner);

			if (
				clippingParentResult.clippingParent != null
				&& owner is FrameworkElement tfe
#if DEBUG
				&& (CanClipSelector?.Invoke(owner) ?? true)
#endif
			)
			{
				var clippingParent = clippingParentResult.clippingParent;

				if (clippingParentResult.child is FrameworkElement cfe && !cfe.ClipChildrenToBounds)
				{
					// If the immediate child of the clipping parent is not clipping its children, then the
					// clipping parent is its parent (Ellipse -> Canvas -> Grid -> StackPanel)
					clippingParent = clippingParentResult.clippingParent.Superview;
				}

				var absolutePosition = ConvertOriginPointToView(owner, clippingParent);

				var clippingBounds = clippingParent.Bounds;

				if (clippingBounds != CGRect.Empty)
				{
					var maskLayer = new CoreAnimation.CAShapeLayer();
					var maskRect = new CGRect(-absolutePosition.X, -absolutePosition.Y, clippingBounds.Width, clippingBounds.Height);
					maskLayer.Path = UIBezierPath.FromRect(maskRect).CGPath;
					layer.Mask = maskLayer;

					if (typeof(UIElement).Log().IsEnabled(LogLevel.Debug))
					{
						typeof(UIElement).Log().LogDebug($"UpdateMask o:{owner.GetType()} p:{clippingParent.GetType()} f:{owner.Frame} b:{owner.Bounds} m:{maskRect} t:{owner.Transform}");
					}

					CreateDebugLayer(owner, layer, absolutePosition, clippingBounds);

					return clippingParent as FrameworkElement;
				}
				else
				{
					CreateDebugLayer(owner, layer, absolutePosition, clippingBounds);

					if (typeof(UIElement).Log().IsEnabled(LogLevel.Debug))
					{
						typeof(UIElement).Log().LogDebug($"Mask disabled for CGRect.Empty parent o:{owner.GetType()}/{owner.GetHashCode():X8} p:{clippingParent.GetType()}/{clippingParent.GetHashCode():X8} f:{owner.Frame} b:{owner.Bounds} t:{owner.Transform}");
					}
				}
			}

			layer.Mask = null;

			return null;
		}

		private static void CreateDebugLayer(UIView owner, CALayer layer, CGPoint absolutePosition, CGRect clippingBounds)
		{
			if (FeatureConfiguration.UIElement.ShowClippingBounds)
			{
				if (_debugLayers == null)
				{
					_debugLayers = new Dictionary<UIView, CALayer>();
				}

				if (_debugLayers.TryGetValue(owner, out var previousLayer))
				{
					previousLayer.RemoveFromSuperLayer();
					_debugLayers.Remove(owner);
				}

				var debugLayer = new CoreAnimation.CAShapeLayer();
				var debugMaskRect = new CGRect(
					-absolutePosition.X,
					-absolutePosition.Y,
					clippingBounds.Width < 1 ? 5 : clippingBounds.Width,
					clippingBounds.Height < 1 ? 5 : clippingBounds.Height
				);

				debugLayer.Path = UIBezierPath.FromRect(debugMaskRect).CGPath;
				debugLayer.Frame = debugMaskRect;
				debugLayer.LineWidth = 2;

				if (clippingBounds.Width == 0 && clippingBounds.Height == 0)
				{
					debugLayer.StrokeColor = Colors.Red;
				}
				else if (clippingBounds.Width < 1 || clippingBounds.Height < 1)
				{
					debugLayer.StrokeColor = Colors.Purple;
				}
				else
				{
					debugLayer.StrokeColor = Colors.Blue;
				}

				debugLayer.Opaque = false;
				debugLayer.BackgroundColor = UIColor.Clear.CGColor;
				debugLayer.FillColor = UIColor.Clear.CGColor;
				debugLayer.MasksToBounds = false;

				_debugLayers.Add(owner, debugLayer);

				if (layer.Sublayers != null)
				{
					layer.InsertSublayer(debugLayer, layer.Sublayers.Length);
				}
				else
				{
					layer.AddSublayer(debugLayer);
				}
			}
		}

		/// <summary>
		/// Gets the origin point of the <paramref name="view"/> in the clippingParent's 
		/// coordinate system.
		/// </summary>
		/// <param name="view">The view to get the point from</param>
		/// <param name="parentView">The view for which to get the adjusted coordinates from</param>
		/// <returns></returns>
		private static CGPoint ConvertOriginPointToView(UIView view, UIView parentView)
		{
			var value = CGPoint.Empty;
			var current = view;

			do
			{
				if (current is FrameworkElement fr)
				{
					value.X += (nfloat)fr.AppliedFrame.X;
					value.Y += (nfloat)fr.AppliedFrame.Y;
				}
				else
				{
					value.X += current.Frame.X;
					value.Y += current.Frame.Y;
				}

				current = current.Superview;

			} while (current != null && current != parentView);

			return value;
		}

#if DEBUG
		public static Predicate<UIView> ViewOfInterestSelector { get; set; } = v => (v as FrameworkElement)?.Name == "TargetView";

		public bool IsViewOfInterest => ViewOfInterestSelector(this);

		/// <summary>
		/// Returns all views matching <see cref="ViewOfInterestSelector"/> anywhere in the visual tree. Handy when debugging Uno.
		/// </summary>
		/// <remarks>This property is intended as a shortcut to inspect the properties of a specific view at runtime. Suggested usage: 
		/// 1. Be debugging Uno. 2. Flag the view you want in xaml with 'Name = "TargetView", or set <see cref="ViewOfInterestSelector"/> 
		/// to select the view you want. 3. Put a breakpoint in the <see cref="FrameworkElement.HitTest(CGPoint, UIEvent)"/> method. 4. Tap anywhere in the app. 
		/// 5. Inspect this property, or one of the typed versions below.</remarks>
		public UIView[] ViewsOfInterest
		{
			get
			{
				UIView topLevel = this;

				while (topLevel.Superview is UIView newTopLevel)
				{
					topLevel = newTopLevel;
				}

				return GetMatchesInChildren(topLevel).ToArray();

				IEnumerable<UIView> GetMatchesInChildren(UIView parent)
				{
					foreach (var subview in parent.Subviews)
					{
						if (ViewOfInterestSelector(subview))
						{
							yield return subview;
						}

						foreach (var match in GetMatchesInChildren(subview))
						{
							yield return match;
						}
					}
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

			UIView topLevel = this;

			if (!searchDescendantsOnly)
			{
				while (topLevel.Superview is UIView newTopLevel)
				{
					topLevel = newTopLevel;
				}
			}

			return GetMatchesInChildren(topLevel).ToArray();

			IEnumerable<FrameworkElement> GetMatchesInChildren(UIView parent)
			{
				foreach (var subview in parent.Subviews)
				{
					if (subview is FrameworkElement fe && fe.Name == name)
					{
						yield return fe;
					}

					foreach (var match in GetMatchesInChildren(subview))
					{
						yield return match;
					}
				}
			}
		}

		public FrameworkElement[] FrameworkElementsOfInterest => ViewsOfInterest.OfType<FrameworkElement>().ToArray();

		public Controls.ContentControl[] ContentControlsOfInterest => ViewsOfInterest.OfType<Controls.ContentControl>().ToArray();

		public Controls.Panel[] PanelsOfInterest => ViewsOfInterest.OfType<Controls.Panel>().ToArray();

		/// <summary>
		/// Strongly-typed superview, purely a debugger convenience.
		/// </summary>
		public FrameworkElement FrameworkElementSuperview => Superview as FrameworkElement;

		public string ShowDescendants() => UIViewExtensions.ShowDescendants(this);

		public string ShowLocalVisualTree(int fromHeight) => UIViewExtensions.ShowLocalVisualTree(this, fromHeight);

		public IList<VisualStateGroup> VisualStateGroups => VisualStateManager.GetVisualStateGroups((this as Controls.Control).GetTemplateRoot());
#endif
	}
}
