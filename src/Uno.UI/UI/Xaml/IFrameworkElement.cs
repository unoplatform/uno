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

#if XAMARIN_ANDROID
using View = Android.Views.View;
using Font = Android.Graphics.Typeface;
using Android.Graphics;
using nint = System.Int32;
using nfloat = System.Double;
using NMath = System.Math;
using CGSize = Windows.Foundation.Size;
using _Size = Windows.Foundation.Size;
using Point = Windows.Foundation.Point;
#elif XAMARIN_IOS_UNIFIED
using View = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
using CoreGraphics;
using _Size = Windows.Foundation.Size;
using Point = Windows.Foundation.Point;
#if NET6_0_OR_GREATER
using ObjCRuntime;
#endif
#elif __MACOS__
using AppKit;
using View = AppKit.NSView;
using Color = AppKit.NSColor;
using Font = AppKit.NSFont;
using CoreGraphics;
using _Size = Windows.Foundation.Size;
using Point = Windows.Foundation.Point;
#if NET6_0_OR_GREATER
using ObjCRuntime;
#endif
#elif __WASM__
using nint = System.Int32;
using nfloat = System.Double;
using Point = Windows.Foundation.Point;
using CGSize = Windows.Foundation.Size;
using _Size = Windows.Foundation.Size;
using NMath = System.Math;
using View = Windows.UI.Xaml.UIElement;
#else
using nint = System.Int32;
using nfloat = System.Double;
using CGSize = Windows.Foundation.Size;
using _Size = Windows.Foundation.Size;
using NMath = System.Math;
using View = Windows.UI.Xaml.UIElement;
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

		bool IsEnabled { get; set; }

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

#if XAMARIN_IOS || __MACOS__

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
#if XAMARIN_ANDROID
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

#elif XAMARIN_IOS
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

		public static IFrameworkElement FindName(IFrameworkElement e, IEnumerable<View> subviews, string name)
		{
			if (string.Equals(e.Name, name, StringComparison.Ordinal))
			{
				return e;
			}

			var frameworkElements = subviews
				.Safe()
				.OfType<IFrameworkElement>()
				.Reverse()
				.ToArray();

			if (frameworkElements.Length == 0)
			{
				// If element is a ContentControl with a view as Content, include the view and its children in the search,
				// to better match Windows behaviour
				var content =
					(e as ContentControl)?.Content as IFrameworkElement ??
					(e as Controls.Primitives.Popup)?.Child as IFrameworkElement;

				if (content != null)
				{
					frameworkElements = new IFrameworkElement[] { content };
				}
			}

			foreach (var frameworkElement in frameworkElements)
			{
				if (string.Equals(frameworkElement.Name, name, StringComparison.Ordinal))
				{
					return frameworkElement.ConvertFromStubToElement(e, name);
				}
			}

			foreach (var frameworkElement in frameworkElements)
			{
				var subviewResult = frameworkElement.FindName(name) as IFrameworkElement;
				if (subviewResult != null)
				{
					return subviewResult.ConvertFromStubToElement(e, name);
				}
			}

			if(e is UIElement uiElement && uiElement.ContextFlyout is Controls.Primitives.FlyoutBase contextFlyout)
			{
				return FindInFlyout(name, contextFlyout);
			}

			if (e is Button button && button.Flyout is Controls.Primitives.FlyoutBase buttonFlyout)
			{
				return FindInFlyout(name, buttonFlyout);
			}

			return null;
		}

		private static IFrameworkElement FindInFlyout(string name, Controls.Primitives.FlyoutBase flyoutBase)
			=> flyoutBase switch
			{
				MenuFlyout f => f.Items.Select(i => i.FindName(name) as IFrameworkElement).Trim().FirstOrDefault(),
				Controls.Primitives.FlyoutBase fb => fb.GetPresenter()?.FindName(name) as IFrameworkElement
			};

		public static CGSize Measure(this IFrameworkElement element, _Size availableSize)
		{
#if XAMARIN_IOS || __MACOS__
			return ((View)element).SizeThatFits(new CoreGraphics.CGSize(availableSize.Width, availableSize.Height));
#elif XAMARIN_ANDROID
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

#if XAMARIN_IOS || __MACOS__
		private static nfloat NumberOrDefault(this double number, nfloat defaultValue)
		{
			return NumberOrDefault((nfloat)number, defaultValue);
		}
#endif

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

#if XAMARIN_ANDROID
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

