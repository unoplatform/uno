using System;
using System.Linq;
using Uno.Extensions;
using Windows.UI.Xaml;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Data;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Controls;
using Uno.UI;
using Windows.Foundation;
using Windows.UI.Xaml.Controls.Primitives;
using Uno.Foundation.Logging;
using Uno.Collections;

#if __ANDROID__
using View = Android.Views.View;
using ViewGroup = Android.Views.ViewGroup;
using Font = Android.Graphics.Typeface;
using Android.Graphics;
#pragma warning disable CS8981 // The type name 'nint' only contains lower-cased ascii characters. Such names may become reserved for the language
using nint = System.Int32;
using nfloat = System.Double;
using NMath = System.Math;
using CGSize = Windows.Foundation.Size;
using _Size = Windows.Foundation.Size;
using Point = Windows.Foundation.Point;
#elif __IOS__
using View = UIKit.UIView;
using ViewGroup = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
using CoreGraphics;
using _Size = Windows.Foundation.Size;
using Point = Windows.Foundation.Point;
using ObjCRuntime;
#elif __MACOS__
using AppKit;
using View = AppKit.NSView;
using ViewGroup = AppKit.NSView;
using Color = AppKit.NSColor;
using Font = AppKit.NSFont;
using CoreGraphics;
using _Size = Windows.Foundation.Size;
using Point = Windows.Foundation.Point;
using ObjCRuntime;
#elif __WASM__
#pragma warning disable CS8981 // The type name 'nint' only contains lower-cased ascii characters. Such names may become reserved for the language
using nint = System.Int32;
using nfloat = System.Double;
using Point = Windows.Foundation.Point;
using CGSize = Windows.Foundation.Size;
using _Size = Windows.Foundation.Size;
using NMath = System.Math;
using View = Windows.UI.Xaml.UIElement;
using ViewGroup = Windows.UI.Xaml.UIElement;
#else
#pragma warning disable CS8981 // The type name 'nint' only contains lower-cased ascii characters. Such names may become reserved for the language
using nint = System.Int32;
using nfloat = System.Double;
using CGSize = Windows.Foundation.Size;
using _Size = Windows.Foundation.Size;
using NMath = System.Math;
using View = Windows.UI.Xaml.UIElement;
using ViewGroup = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml
{
	internal partial interface IFrameworkElement : IDataContextProvider, DependencyObject, IDependencyObjectParse
	{
		event RoutedEventHandler Loaded;
		event RoutedEventHandler Unloaded;
		event EventHandler<object> LayoutUpdated;
		event SizeChangedEventHandler SizeChanged;

		object FindName(string name);

		DependencyObject Parent { get; }

		string Name { get; set; }

		Visibility Visibility { get; set; }

		Thickness Margin { get; set; }

		double Width { get; set; }

		double Height { get; set; }

		double MinWidth { get; set; }

		double MinHeight { get; set; }

		double MaxWidth { get; set; }

		double MaxHeight { get; set; }

		double ActualWidth { get; }

		double ActualHeight { get; }

		double Opacity { get; set; }

		Style Style { get; set; }

		Windows.UI.Xaml.Media.Brush Background { get; set; }

		Transform RenderTransform { get; set; }

#if XAMARIN || __WASM__
		Point RenderTransformOrigin { get; set; }
#endif
		TransitionCollection Transitions { get; set; }

		HorizontalAlignment HorizontalAlignment { get; set; }

		VerticalAlignment VerticalAlignment { get; set; }

		Uri BaseUri { get; }

		_Size AdjustArrange(_Size finalSize);

		int? RenderPhase { get; set; }

		DependencyObject TemplatedParent { get; set; }

		void ApplyBindingPhase(int phase);

		// void OnLoaded ();
		// void OnUnloaded ();
		// void OnLayoutUpdated ();
		// Windows.Foundation.Size SizeThatFits (Windows.Foundation.Size size);
		// void SetNeedsLayout ();
		// void SetSuperviewNeedsLayout ();

#if __IOS__ || __MACOS__

		/// <summary>
		/// The frame applied to this child when last arranged by its parent. This may differ from the current UIView.Frame if a RenderTransform is set.
		/// </summary>
		Rect AppliedFrame { get; }

		void SetSubviewsNeedLayout();
#endif

		AutomationPeer GetAutomationPeer();

		string GetAccessibilityInnerText();
	}

	internal static class IFrameworkElementHelper
	{
		/// <summary>
		/// Initializes the standard properties for this framework element.
		/// </summary>
		public static void Initialize(IFrameworkElement e)
		{
#if __IOS__
			if (e is UIElement uiElement)
			{
				uiElement.ClipsToBounds = false;
				uiElement.Layer.MasksToBounds = false;
				uiElement.Layer.MaskedCorners = (CoreAnimation.CACornerMask)0;
			}
#endif

#if __ANDROID__
			if (e is View view)
			{
				// TODO:
				// This causes Android to cycle through every visible IFrameworkElement every time the accessibility focus is changed
				// and calls the overridden InitializeAccessibilityNodeInfo method.
				// This could become a performance problem (due to interop) in complex UIs (only if TalkBack is enabled).
				// A possible optimization could be to set it to ImportantForAccessibility.No if FrameworkElement.CreateAutomationPeer returns null.
				view.ImportantForAccessibility = Android.Views.ImportantForAccessibility.Yes;
			}
#endif

			if (e is IFrameworkElement_EffectiveViewport evp)
			{
				evp.InitializeEffectiveViewport();
			}
		}

		/// <summary>
		/// Finds a realised element in the control template
		/// </summary>
		/// <param name="e">The framework element instance</param>
		/// <param name="name">The name of the template part</param>
		public static DependencyObject GetTemplateChild(this IFrameworkElement e, string name)
		{
			return e.FindName(name) as IFrameworkElement;
		}
#if !UNO_REFERENCE_API
		// This extension method is not needed for Skia
		// nor Wasm, since all elements in visual tree are
		// of type UIElement
		public static void InvalidateMeasure(this IFrameworkElement e)
		{
			switch (e)
			{
				case FrameworkElement fe:
					fe.InvalidateMeasure();
					break;
				case View view:
#if __ANDROID__
					view.RequestLayout();

					// Invalidate the first "managed" parent to
					// ensure its .MeasureOverride() gets called
					var parent = view.Parent;
					while (parent is { })
					{
						if (parent is UIElement uie)
						{
							uie.InvalidateMeasure();
							break;
						}

						parent = parent.Parent;
					}

#elif __IOS__
					view.SetNeedsLayout();
#elif __MACOS__
					view.NeedsLayout = true;
#endif
					break;

				default:
					e.Log().Warn("Calling InvalidateMeasure on a UIElement that is not a FrameworkElement has no effect.");
					break;
			}
		}
#endif

		public static IFrameworkElement FindName(IFrameworkElement e, ViewGroup group, string name)
		{
			if (string.Equals(e.Name, name, StringComparison.Ordinal))
			{
				return e;
			}

			// The lambda is static to make sure it doesn't capture anything, for performance reasons.
			var matchingChild = group.FindLastChild(name, static (c, name) => c is IFrameworkElement fe && string.Equals(fe.Name, name, StringComparison.Ordinal) ? fe : null, out var hasAnyChildren);
			if (hasAnyChildren)
			{
				matchingChild ??= group.FindLastChild(name, static (c, name) => (c as IFrameworkElement)?.FindName(name) as IFrameworkElement, out _);
			}

			if (matchingChild is not null)
			{
				return matchingChild.ConvertFromStubToElement(e, name);
			}

			// If element is a ContentControl with a view as Content, include the view and its children in the search,
			// to better match Windows behaviour
			IFrameworkElement content = null;
			if (!hasAnyChildren &&
				e is ContentControl contentControl &&
				contentControl.Content is IFrameworkElement innerContent &&
				contentControl.ContentTemplate is null) // Only include the Content view if there is no ContentTemplate.
			{
				content = innerContent;
			}
			else if (!hasAnyChildren &&
				e is Controls.Primitives.Popup popup)
			{
				content = popup.Child as IFrameworkElement;
			}

			if (content != null)
			{
				if (string.Equals(content.Name, name, StringComparison.Ordinal))
				{
					return content.ConvertFromStubToElement(e, name);
				}

				var subviewResult = content.FindName(name) as IFrameworkElement;
				if (subviewResult != null)
				{
					return subviewResult.ConvertFromStubToElement(e, name);
				}
			}

			if (__LinkerHints.Is_Windows_UI_Xaml_Controls_Primitives_FlyoutBase_Available)
			{
				// Static version here to ensure that it's not used outside of this scope
				// where we're ensuring that we're not taking a dependency on FlyoutBase statically.
				static IFrameworkElement FindInFlyout(string name, Controls.Primitives.FlyoutBase flyoutBase)
					=> flyoutBase switch
					{
						MenuFlyout f => f.Items.Select(i => i.FindName(name) as IFrameworkElement).Trim().FirstOrDefault(),
						Controls.Primitives.FlyoutBase fb => fb.GetPresenter()?.FindName(name) as IFrameworkElement
					};

				if (e is UIElement uiElement && uiElement.ContextFlyout is Controls.Primitives.FlyoutBase contextFlyout)
				{
					return FindInFlyout(name, contextFlyout);
				}

				if (e is Button button && button.Flyout is Controls.Primitives.FlyoutBase buttonFlyout)
				{
					return FindInFlyout(name, buttonFlyout);
				}
			}

			return null;
		}

		public static CGSize Measure(this IFrameworkElement element, _Size availableSize)
		{
#if __IOS__ || __MACOS__
			return ((View)element).SizeThatFits(new CoreGraphics.CGSize(availableSize.Width, availableSize.Height));
#elif __ANDROID__
			var widthSpec = ViewHelper.SpecFromLogicalSize(availableSize.Width);
			var heightSpec = ViewHelper.SpecFromLogicalSize(availableSize.Height);

			var view = ((View)element);
			view.Measure(widthSpec, heightSpec);

			return Uno.UI.Controls.BindableView.GetNativeMeasuredDimensionsFast(view)
				.PhysicalToLogicalPixels();
#else
			return default(CGSize);
#endif
		}

#if __MACOS__
		public static CGSize SizeThatFits(this View element, _Size availableSize)
		{
			switch (element)
			{
				case NSControl nsControl:
					return nsControl.SizeThatFits(availableSize);

				case FrameworkElement fe:
					{
						fe.XamlMeasure(availableSize);
						var desiredSize = fe.DesiredSize;
						return new CGSize(desiredSize.Width, desiredSize.Height);
					}

				case IHasSizeThatFits scp:
					return scp.SizeThatFits(availableSize);

				case View nsview:
					return nsview.FittingSize;

				default:
					throw new NotSupportedException($"Unsupported measure for {element}");
			}
		}

#endif

		public static CGSize SizeThatFits(IFrameworkElement e, CGSize size)
		{
			// Note that on iOS, the computation is intentionally kept as nfloat
			// to handle discrepancies with the nfloat.NaN and double.NaN.

			if (e.Visibility == Visibility.Collapsed)
			{
				return new CGSize(0, 0);
			}

			var (min, max) = e.GetMinMax();

			var width = size
				.Width
				.NumberOrDefault(nfloat.PositiveInfinity)
				.LocalMin((nfloat)max.Width)
				.LocalMax((nfloat)min.Width);

			var height = size
				.Height
				.NumberOrDefault(nfloat.PositiveInfinity)
				.LocalMin((nfloat)max.Height)
				.LocalMax((nfloat)min.Height);

			return new CGSize(width, height);
		}

		/// <summary>
		/// Gets the min value being left or right.
		/// </summary>
		/// <remarks>
		/// This method kept here for readbility
		/// of <see cref="SizeThatFits(IFrameworkElement, CGSize)"/> the keep its
		/// fluent aspect.
		/// It also does not use the generic extension that may create an very
		/// short lived <see cref="IConvertible"/> instance.
		/// </remarks>
		private static nfloat LocalMin(this nfloat left, nfloat right)
		{
			return NMath.Min(left, right);
		}

		/// <summary>
		/// Gets the max value being left or right.
		/// </summary>
		/// <remarks>
		/// This method kept here for readability
		/// of <see cref="SizeThatFits(IFrameworkElement, CGSize)"/> the keep its
		/// fluent aspect.
		/// It also does not use the generic extension that may create an very
		/// short lived <see cref="IConvertible"/> instance.
		/// </remarks>
		private static nfloat LocalMax(this nfloat left, nfloat right)
		{
			return NMath.Max(left, right);
		}

		private static IFrameworkElement ConvertFromStubToElement(this IFrameworkElement element, IFrameworkElement originalRootElement, string name)
		{
			var elementStub = element as ElementStub;
			if (elementStub != null)
			{
				elementStub.Materialize();
				element = originalRootElement.FindName(name) as IFrameworkElement;
			}
			return element;
		}

		private static nfloat NumberOrDefault(this nfloat number, nfloat defaultValue)
		{
			return nfloat.IsNaN(number)
				? defaultValue
				: number;
		}

		public static void MaybeOrNot<TInstance>(this TInstance instance, Action nonNullAction, Action nullAction)
		{
			// Analysis disable once CompareNonConstrainedGenericWithNull
			if (instance != null)
			{
				nonNullAction.Invoke();
			}
			else
			{
				nullAction.Invoke();
			}
		}

#if __ANDROID__
		/// <summary>
		/// Applies the framework element constraints like the size and max size, using an already measured view.
		/// </summary>
		/// <param name="view"></param>
		public static void OnMeasureOverride<T>(T view)
			where T : View, IFrameworkElement
		{
			var updated = IFrameworkElementHelper
				.SizeThatFits(view, new _Size(view.MeasuredWidth, view.MeasuredHeight).PhysicalToLogicalPixels())
				.LogicalToPhysicalPixels();

			Windows.UI.Xaml.Controls.Layouter.SetMeasuredDimensions(view, (int)updated.Width, (int)updated.Height);
		}

		/// <summary>
		/// Applies the framework element constraints like the size and max size, using the provided measured size.
		/// </summary>
		/// <param name="view"></param>
		public static void OnMeasureOverride<T>(T view, _Size measuredSize)
			where T : View, IFrameworkElement
		{
			var updated = IFrameworkElementHelper
				.SizeThatFits(view, new _Size(measuredSize.Width, measuredSize.Height).PhysicalToLogicalPixels())
				.LogicalToPhysicalPixels();

			Windows.UI.Xaml.Controls.Layouter.SetMeasuredDimensions(view, (int)updated.Width, (int)updated.Height);
		}
#endif

		/// <summary>
		/// Base constraint reasoning for simple containers that always respect the stretch of their children.
		/// </summary>
		public static bool? IsWidthConstrainedSimple(this IFrameworkElement element)
		{
			if (!double.IsNaN(element.Width) && !double.IsPositiveInfinity(element.Width))
			{
				//Yes, fixed width
				return true;
			}
			if (element.HorizontalAlignment != HorizontalAlignment.Stretch)
			{
				//No, not taking all available space
				return false;
			}
			//Don't know, ask parent
			return null;
		}

		/// <summary>
		/// Base constraint reasoning for simple containers that always respect the stretch of their children.
		/// </summary>
		public static bool? IsHeightConstrainedSimple(this IFrameworkElement element)
		{
			if (!double.IsNaN(element.Height) && !double.IsPositiveInfinity(element.Height))
			{
				//Yes, fixed Height
				return true;
			}
			if (element.VerticalAlignment != VerticalAlignment.Stretch)
			{
				//No, not taking all available space
				return false;
			}
			//Don't know, ask parent
			return null;
		}
	}
}

