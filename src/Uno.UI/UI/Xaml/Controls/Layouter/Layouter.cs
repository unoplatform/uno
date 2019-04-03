// #define LOG_LAYOUT
#if !__WASM__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Extensions;
using Uno;
using Uno.Logging;
using Uno.Collections;
using static System.Double;
using static System.Math;
using Uno.Diagnostics.Eventing;
using Windows.Foundation;

#if XAMARIN_ANDROID
using View = Android.Views.View;
using Font = Android.Graphics.Typeface;
#elif XAMARIN_IOS_UNIFIED
using View = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
using CoreGraphics;
#elif __MACOS__
using View = AppKit.NSView;
using Color = AppKit.NSColor;
using Font = AppKit.NSFont;
using CoreGraphics;
#elif XAMARIN_IOS
using CoreGraphics;
using View = MonoTouch.UIKit.UIView;
using Color = MonoTouch.UIKit.UIColor;
using Font = MonoTouch.UIKit.UIFont;
#elif NET461 || __WASM__
using View = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml.Controls
{
	public abstract partial class Layouter : ILayouter
	{
		private readonly static IEventProvider _trace = Tracing.Get(FrameworkElement.TraceProvider.Id);

		private readonly IFrameworkElement _element;

		public IFrameworkElement Panel { get { return _element; } }

		public Layouter(IFrameworkElement element)
		{
			_element = element;
		}
		
		/// <summary>
		/// Determine the size of the panel.
		/// </summary>
		/// <param name="availableSize">The available size, in logical pixels.</param>
		/// <returns>The size of the panel, in logical pixel.</returns>
		public Size Measure(Size availableSize)
		{
			IDisposable traceActivity = null;
			if (_trace.IsEnabled)
			{
				traceActivity = _trace.WriteEventActivity(
					FrameworkElement.TraceProvider.FrameworkElement_MeasureStart,
					FrameworkElement.TraceProvider.FrameworkElement_MeasureStop,
					new object[] { LoggingOwnerTypeName, Panel.GetDependencyObjectId() }
				);
			}

			using (traceActivity)
			{
				//Constrain the size of the slot to the child's own constraints (it will not do it by itself)
				var constrainedSize = GetConstrainedSize(availableSize);

				var measuredSize = MeasureOverride(constrainedSize);

				//Constrain the output of the child's measure, as will not have applied its own
				//MaxWidth/MaxHeight/MinWidth/MinHeight/Width/Height
				var size = GetConstrainedSize(measuredSize);

				var desiredSize = size;

				if (this.Panel is FrameworkElement frameworkElement && frameworkElement.Visibility == Visibility.Visible)
				{
					// DesiredSize must include margins
					// However, we report the size to the parent without the margins
					var margin = frameworkElement.Margin;

					// This condition is required because of the measure caching that 
					// some systems apply (Like android UI).
					if (margin != Thickness.Empty)
					{
						desiredSize = new Size(
							desiredSize.Width + margin.Left + margin.Right,
							desiredSize.Height + margin.Top + margin.Bottom
						);
					}
				}

				SetDesiredChildSize(this.Panel as View, desiredSize);

				return size;
			}
		}

		/// <summary>
		/// Places the children of the panel using a specific size, in logical pixels.
		/// </summary>
		public void Arrange(Rect finalRect)
		{
			if (_element is UIElement ui)
			{
				ui.LayoutSlot = finalRect;
			}

			IDisposable traceActivity = null;
			if (_trace.IsEnabled)
			{
				traceActivity = _trace.WriteEventActivity(
					FrameworkElement.TraceProvider.FrameworkElement_ArrangeStart,
					FrameworkElement.TraceProvider.FrameworkElement_ArrangeStop,
					new object[] { LoggingOwnerTypeName, Panel.GetDependencyObjectId() }
				);
			}
			using (traceActivity)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().DebugFormat("[{0}/{1}] Arrange({2}/{3}/{4}/{5})", LoggingOwnerTypeName, Name, GetType(), Panel.Name, finalRect, Panel.Margin);
				}

				ArrangeOverride(finalRect.Size);
			}
		}


		/// <summary>
		/// Determine the size of the panel.
		/// </summary>
		/// <param name="availableSize">The available size, in logical pixels.</param>
		/// <returns>The size of the panel, in logical pixel.</returns>
		protected abstract Size MeasureOverride(Size availableSize);

		/// <summary>
		/// Places the children of the panel using a specific size, in logical pixels.
		/// </summary>
		/// <param name="finalSize">The final panel size</param>
		protected abstract Size ArrangeOverride(Size finalSize);

		/// <summary>
		/// Sets the desired child size back on the view. (Used in iOS which does not store measured size)
		/// </summary>
		partial void SetDesiredChildSize(View view, Size desiredSize);

		protected Size MeasureChild(View view, Size slotSize)
		{
			var frameworkElement = view as IFrameworkElement;
			var ret = default(Size);
			if (frameworkElement != null)
			{
				var margin = frameworkElement.Margin;

				if (frameworkElement.Visibility == Visibility.Collapsed)
				{
					// By default iOS views measure to normal size, even if they're hidden.
					// We want the collapsed behavior, so we return a 0,0 size instead.
					SetDesiredChildSize(view, ret);

					if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
					{
						var viewName = frameworkElement.SelectOrDefault(f => f.Name, "NativeView");

						this.Log().DebugFormat(
							"[{0}/{1}] MeasureChild(HIDDEN/{2}/{3}/{4}/{5}) = {6}",
							LoggingOwnerTypeName,
							Name,
							view.GetType(),
							viewName,
							slotSize,
							margin,
							ret
						);
					}

					return ret;
				}

				if (margin != Thickness.Empty)
				{
					// Apply the margin for framework elements, as if it were padding to the child.
					slotSize = new Size(
						Max(0, slotSize.Width - margin.Left - margin.Right),
						Max(0, slotSize.Height - margin.Top - margin.Bottom)
					);
				}

				// Alias the Dependency Properties values to avoid double calls.
				var childWidth = frameworkElement.Width;
				var childMaxWidth = frameworkElement.MaxWidth;
				var childHeight = frameworkElement.Height;
				var childMaxHeight = frameworkElement.MaxHeight;

				var optionalMaxWidth = !IsInfinity(childMaxWidth) && !IsNaN(childMaxWidth) ? childMaxWidth : (double?)null;
				var optionalWidth = !IsNaN(childWidth) ? childWidth : (double?)null;
				var optionalMaxHeight = !IsInfinity(childMaxHeight) && !IsNaN(childMaxHeight) ? childMaxHeight : (double?)null;
				var optionalHeight = !IsNaN(childHeight) ? childHeight : (double?)null;

				// After the margin has been removed, ensure the remaining space slot does not go
				// over the explicit or maximum size of the child.
				if (optionalMaxWidth != null || optionalWidth != null)
				{
					var constrainedWidth = Min(
						optionalMaxWidth ?? double.PositiveInfinity,
						optionalWidth ?? double.PositiveInfinity
					);

					slotSize.Width = Min(slotSize.Width, constrainedWidth);
				}

				if (optionalMaxHeight != null || optionalHeight != null)
				{
					var constrainedHeight = Min(
						optionalMaxHeight ?? double.PositiveInfinity,
						optionalHeight ?? double.PositiveInfinity
					);

					slotSize.Height = Min(slotSize.Height, constrainedHeight);
				}
			}

			ret = MeasureChildOverride(view, slotSize);

			if (
				IsPositiveInfinity(ret.Height) || IsPositiveInfinity(ret.Width)
			)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
				{
					var viewName = frameworkElement.SelectOrDefault(f => f.Name, "NativeView");
					var margin = frameworkElement.SelectOrDefault(f => f.Margin, Thickness.Empty);

					this.Log().ErrorFormat(
						"[{0}/{1}] MeasureChild({2}/{3}/{4}/{5}) = Child returned INFINITY {6}",
						LoggingOwnerTypeName,
						Name,
						view.GetType(),
						viewName,
						slotSize,
						margin,
						ret
					);
				}

				ret = new Size(0, 0);
			}
			else
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					var viewName = frameworkElement.SelectOrDefault(f => f.Name, "NativeView");
					var margin = frameworkElement.SelectOrDefault(f => f.Margin, Thickness.Empty);

					this.Log().DebugFormat(
						"[{0}/{1}] MeasureChild({2}/{3}/{4}/{5}) = {6}",
						LoggingOwnerTypeName,
						Name,
						view.GetType(),
						viewName,
						slotSize,
						margin,
						ret
					);
				}
			}

			if (frameworkElement != null)
			{
				var margin = frameworkElement.Margin;

				if (margin != Thickness.Empty)
				{
					// Report the size to the parent without the margin, only if the
					// size has changed or that the control required a measure
					//
					// This condition is required because of the measure caching that 
					// some systems apply (Like android UI).
					ret = new Size(
						ret.Width + margin.Left + margin.Right,
						ret.Height + margin.Top + margin.Bottom
					);
				}
			}

			SetDesiredChildSize(view, ret);

			return ret;
		}

		/// <summary>
		/// Arranges the location a child in the current panel
		/// </summary>
		/// <param name="view">The child instance</param>
		/// <param name="frame">The rectangle to use, in Logical position</param>
		public void ArrangeChild(View view, Rect frame)
		{
			if ((view as IFrameworkElement)?.Visibility == Visibility.Collapsed)
			{
				return;
			}
			frame = ApplyMarginAndAlignments(view, frame);

			ArrangeChildOverride(view, frame);
		}

		private void LogArrange(View view, Rect frame)
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				var viewName = (view as IFrameworkElement).SelectOrDefault(f => f.Name, "NativeView");
				var margin = (view as IFrameworkElement).SelectOrDefault(f => f.Margin, Thickness.Empty);

				this.Log().DebugFormat("[{0}/{1}] ArrangeChild({2}/{3}/{4}/{5})", LoggingOwnerTypeName, Name, view.GetType(), viewName, frame, margin);
			}

		}

		protected Thickness MarginChild(View view)
		{
			var frameworkElement = view as IFrameworkElement;

			if (frameworkElement != null)
			{
				return frameworkElement.Margin;
			}
			else
			{
				return Thickness.Empty;
			}
		}

		protected abstract string Name { get; }

		private Rect ApplyMarginAndAlignments(View view, Rect frame)
		{
			// In this implementation, since we do not have the ability to intercept proprely the measure and arrange
			// because of the type of hierarchy (inherting from native views), we must apply the margins and alignements
			// from within the panel to its children. This makes the authoring of custom panels that do not inherit from 
			// Panel that do not use this helper a bit more complex, but for all other panels that use this
			// layouter, the logic is implied.

			var frameworkElement = view as IFrameworkElement;

			if (frameworkElement != null)
			{
				// Apply the margin for framework elements, as if it were padding to the child.
				double x = frame.X;
				double y = frame.Y;
				double width = frame.Width;
				double height = frame.Height;

				// capture the child's state to avoid getting DependencyProperties values multiple times.
				var childVerticalAlignment = frameworkElement.VerticalAlignment;
				var childHorizontalAlignment = frameworkElement.HorizontalAlignment;
				var childMaxHeight = frameworkElement.MaxHeight;
				var childMaxWidth = frameworkElement.MaxWidth;
				var childMinHeight = frameworkElement.MinHeight;
				var childMinWidth = frameworkElement.MinWidth;
				var childWidth = frameworkElement.Width;
				var childHeight = frameworkElement.Height;
				var childMargin = frameworkElement.Margin;
				var hasChildHeight = !IsNaN(childHeight);
				var hasChildWidth = !IsNaN(childWidth);
				var hasChildMaxWidth = !IsInfinity(childMaxWidth) && !IsNaN(childMaxWidth);
				var hasChildMaxHeight = !IsInfinity(childMaxHeight) && !IsNaN(childMaxHeight);
				var hasChildMinWidth = childMinWidth > 0.0f;
				var hasChildMinHeight = childMinHeight > 0.0f;

				if (
					childVerticalAlignment != VerticalAlignment.Stretch
					|| childHorizontalAlignment != HorizontalAlignment.Stretch
					|| hasChildWidth
					|| hasChildHeight
					|| hasChildMaxWidth
					|| hasChildMaxHeight
					|| hasChildMinWidth
					|| hasChildMinHeight
					)
				{
					var desiredSize = DesiredChildSize(view);

					// Apply vertical alignment
					if (
						childVerticalAlignment != VerticalAlignment.Stretch
						|| hasChildHeight
						|| hasChildMaxHeight
						|| hasChildMinHeight
					)
					{

						var actualHeight = GetActualHeight(height,
							childVerticalAlignment,
							childMaxHeight,
							childMinHeight,
							childHeight,
							childMargin,
							hasChildHeight,
							hasChildMaxHeight,
							hasChildMinHeight,
							desiredSize);

						switch (childVerticalAlignment)
						{
							case VerticalAlignment.Top:
								y = frame.Y;
								break;

							case VerticalAlignment.Bottom:
								y = frame.Y + frame.Height - actualHeight;
								break;

							case VerticalAlignment.Stretch:
							case VerticalAlignment.Center:
								y = frame.Y + (frame.Height - actualHeight) / 2;
								break;
						}

						height = actualHeight;
					}

					// Apply horizontal alignment
					if (
						childHorizontalAlignment != HorizontalAlignment.Stretch
						|| hasChildWidth
						|| hasChildMaxWidth
						|| hasChildMinWidth
					)
					{
						var actualWidth = GetActualWidth(width,
							childHorizontalAlignment,
							childMaxWidth,
							childMinWidth,
							childWidth,
							childMargin,
							hasChildWidth,
							hasChildMaxWidth,
							hasChildMinWidth,
							desiredSize);

						switch (childHorizontalAlignment)
						{
							case HorizontalAlignment.Left:
								x = frame.X;
								break;

							case HorizontalAlignment.Right:
								x = frame.X + frame.Width - actualWidth;
								break;

							case HorizontalAlignment.Stretch:
							case HorizontalAlignment.Center:
								x = frame.X + (frame.Width - actualWidth) / 2;
								break;
						}

						width = actualWidth;
					}
				}

				frame = new Rect(
					x + childMargin.Left,
					y + childMargin.Top,
					width - childMargin.Left - childMargin.Right,
					height - childMargin.Top - childMargin.Bottom
				);

				frame.Size = frameworkElement.AdjustArrange(frame.Size);

			}

			return new Rect(
				x: IsNaN(frame.X) ? 0 : frame.X,
				y: IsNaN(frame.Y) ? 0 : frame.Y,
				width: Max(0, IsNaN(frame.Width) ? 0 : frame.Width),
				height: Max(0, IsNaN(frame.Height) ? 0 : frame.Height)
			);
		}

		/// <summary>
		/// Get actual width based on MinWidth, MaxWidth, Width and HorizontalAlignment
		/// </summary>
		private double GetActualWidth(double width,
			HorizontalAlignment childHorizontalAlignment,
			double childMaxWidth,
			double childMinWidth,
			double childWidth,
			Thickness childMargin,
			bool hasChildWidth,
			bool hasChildMaxWidth,
			bool hasChildMinWidth,
			Size desiredSize)
		{
			//Default value
			//childHorizontalAlignment != HorizontalAlignment.Stretch
			var actualWidth = Min(width, desiredSize.Width);

			if (hasChildWidth)
			{
				actualWidth = Min(childWidth + childMargin.Left + childMargin.Right, width);
			}
			else if (hasChildMaxWidth && hasChildMinWidth)
			{
				actualWidth = Min(childMaxWidth + childMargin.Left + childMargin.Right,
						childHorizontalAlignment == HorizontalAlignment.Stretch
						? width
						: desiredSize.Width
					);

				actualWidth = Max(childMinWidth + childMargin.Left + childMargin.Right, actualWidth);
			}
			else if (hasChildMaxWidth)
			{
				actualWidth = Min(childMaxWidth + childMargin.Left + childMargin.Right,
						childHorizontalAlignment == HorizontalAlignment.Stretch
						? width
						: desiredSize.Width
					);
			}
			else if (hasChildMinWidth)
			{
				actualWidth = Max(childMinWidth + childMargin.Left + childMargin.Right,
						childHorizontalAlignment == HorizontalAlignment.Stretch
						? width
						: desiredSize.Width
					);
			}

			return actualWidth;
		}

		/// <summary>
		/// Get actual height based on MinHeight, MaxHeight, Height and VerticalAlignment
		/// </summary>
		private double GetActualHeight(double height,
			VerticalAlignment childVerticalAlignment,
			double childMaxHeight,
			double childMinHeight,
			double childHeight,
			Thickness childMargin,
			bool hasChildHeight,
			bool hasChildMaxHeight,
			bool hasChildMinHeight,
			Size desiredSize)
		{
			//Default value
			//childVerticalAlignment != VerticalAlignment.Stretch
			var actualHeight = Min(height, desiredSize.Height);

			if (hasChildHeight)
			{
				actualHeight = Min(childHeight + childMargin.Top + childMargin.Bottom, height);
			}
			else if (hasChildMaxHeight && hasChildMinHeight)
			{
				actualHeight = Min(childMaxHeight + childMargin.Top + childMargin.Bottom,
						childVerticalAlignment == VerticalAlignment.Stretch
						? height
						: desiredSize.Height
					);

				actualHeight = Max(childMinHeight + childMargin.Top + childMargin.Bottom, actualHeight);
			}
			else if (hasChildMaxHeight)
			{
				actualHeight = Min(childMaxHeight + childMargin.Top + childMargin.Bottom,
						childVerticalAlignment == VerticalAlignment.Stretch
						? height
						: desiredSize.Height
					);
			}
			else if (hasChildMinHeight)
			{
				actualHeight = Max(childMinHeight + childMargin.Top + childMargin.Bottom,
						childVerticalAlignment == VerticalAlignment.Stretch
						? height
						: desiredSize.Height
					);
			}

			return actualHeight;
		}

		/// <summary>
		/// Measures the specified child.
		/// </summary>
		/// <param name="view">The view to measure</param>
		/// <param name="slotSize">The maximum size the child can use.</param>
		/// <returns>The size the view requires.</returns>
		/// <remarks>
		/// Provides the ability for external implementations to measure children.
		/// Mainly used for compatibility with existing WPF/WinRT implementations.
		/// </remarks>
		void ILayouter.ArrangeChild(View view, Rect frame)
		{
			ArrangeChild(view, frame);
		}

		/// <summary>
		/// Arranges the specified view.
		/// </summary>
		/// <param name="view">The view to arrange</param>
		/// <param name="frame">The frame available for the child.</param>
		/// <remarks>
		/// Provides the ability for external implementations to measure children.
		/// Mainly used for compatibility with existing WPF/WinRT implementations.
		/// </remarks>
		Size ILayouter.MeasureChild(View view, Size slotSize)
		{
			return MeasureChild(view, slotSize);
		}

		private Size GetConstrainedSize(Size availableSize)
		{
			var constrainedSize = IFrameworkElementHelper.SizeThatFits(_element as IFrameworkElement, availableSize);

#if XAMARIN_IOS
			return constrainedSize.ToFoundationSize();
#else
			return constrainedSize;
#endif
		}

		private string LoggingOwnerTypeName => ((object)Panel ?? this).GetType().Name;
	}
}
#endif
