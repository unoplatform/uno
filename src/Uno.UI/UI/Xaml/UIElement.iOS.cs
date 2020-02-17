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
		public UIElement()
		{
			InitializePointers();
		}

		internal bool ClippingIsSetByCornerRadius { get; set; } = false;

		partial void ApplyNativeClip(Rect rect)
		{

			if (rect.IsEmpty
				|| double.IsPositiveInfinity(rect.X)
				|| double.IsPositiveInfinity(rect.Y)
				|| double.IsPositiveInfinity(rect.Width)
				|| double.IsPositiveInfinity(rect.Height)
			)
			{
				if (!ClippingIsSetByCornerRadius)
				{
					this.Layer.Mask = null;
				}
				return;
			}

			this.Layer.Mask = new CAShapeLayer
			{
				Path = CGPath.FromRect(rect.ToCGRect())
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
				InvalidateMeasure();

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
