using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Uno.Collections;
using Uno.Extensions;
using Uno.UI.Extensions;
using Uno.Logging;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Extensions.Logging;
#if XAMARIN_IOS
using UIKit;
#elif __MACOS__
using AppKit;
#endif

#if IS_UNO
using _VisibleBoundsPadding = Uno.UI.Behaviors.InternalVisibleBoundsPadding;
#else
using Uno.UI.Toolkit.Extensions;
using _VisibleBoundsPadding = Uno.UI.Toolkit.VisibleBoundsPadding;
#endif

#if IS_UNO
namespace Uno.UI.Behaviors
{
	/// <summary>
	/// Internal Uno behavior, use VisibleBoundsPadding instead.
	/// </summary>
	/// <remarks>
	/// This class is located in the same source file as the VisibleBoundsPadding class to avoid code duplication.
	/// This is required to ensure that both Uno.UI styles and UWP (through Uno.UI.Toolkit) can use this behavior
	/// and not have to synchronize two code files. The internal implementation is not supposed to be used outside 
	/// of the Uno.UI assembly, Uno.UI.Toolkit.VisibleBoundsPadding should be used by dependents.
	/// </remarks>
	internal static class InternalVisibleBoundsPadding
#else
namespace Uno.UI.Toolkit
{
	/// <summary>
	/// A behavior which automatically adds padding to a control that ensures its content will always be inside 
	/// the <see cref="ApplicationView.VisibleBounds"/> of the application. Set PaddingMask to 'All' to enable this behavior,
	/// or set PaddingMask to another value to enable it only on a particular side or sides.
	/// </summary>
	public static class VisibleBoundsPadding
#endif
	{
#if IS_UNO
		private static readonly Lazy<ILogger> _log = new Lazy<ILogger>(() => typeof(InternalVisibleBoundsPadding).Log());
#else
		private static readonly Lazy<ILogger> _log = new Lazy<ILogger>(() => typeof(VisibleBoundsPadding).Log());
#endif

		[Flags]
		public enum PaddingMask
		{
			All = Left | Right | Top | Bottom,
			None = 0,
			Top = 1,
			Bottom = 2,
			Left = 4,
			Right = 8
		}

		/// <summary>
		/// The padding of the visible area relative to the entire window.
		/// </summary>
		/// <remarks>This will be 0 if the entire window is 'safe' for content.</remarks>
		public static Thickness WindowPadding
		{
			get
			{
				var visibleBounds = ApplicationView.GetForCurrentView().VisibleBounds;
				var bounds = Window.Current.Bounds;
				var result = new Thickness {
					Left = visibleBounds.Left - bounds.Left,
					Top = visibleBounds.Top - bounds.Top,
					Right = bounds.Right - visibleBounds.Right,
					Bottom = bounds.Bottom - visibleBounds.Bottom
				};

				if (_log.Value.IsEnabled(LogLevel.Debug))
				{
					_log.Value.LogDebug($"WindowPadding={result} bounds={bounds} visibleBounds={visibleBounds}");
				}

				return result;
			}
		}

		/// <summary>
		/// VisibleBounds offset to the reference frame of the window Bounds.
		/// </summary>
		private static Rect OffsetVisibleBounds
		{
			get
			{
				var visibleBounds = ApplicationView.GetForCurrentView().VisibleBounds;
				var bounds = Window.Current.Bounds;
				visibleBounds.X -= bounds.X;
				visibleBounds.Y -= bounds.Y;

				return visibleBounds;
			}
		}

		public static PaddingMask GetPaddingMask(DependencyObject obj)
			=> (PaddingMask)obj.GetValue(PaddingMaskProperty);

		/// <summary>
		/// Set the <see cref="PaddingMask"/> to use on this property. A mask of <see cref="PaddingMask.All"/> will apply visible bounds 
		/// padding on all sides, a mask of <see cref="PaddingMask.Bottom"/> will adjust only the bottom padding, etc. The different options 
		/// can be combined as bit flags. 
		/// </summary>
		public static void SetPaddingMask(DependencyObject obj, PaddingMask value)
			=> obj.SetValue(PaddingMaskProperty, value);

		public static DependencyProperty PaddingMaskProperty { get ; } =
			DependencyProperty.RegisterAttached("PaddingMask", typeof(PaddingMask), typeof(_VisibleBoundsPadding), new FrameworkPropertyMetadata(PaddingMask.None, OnIsPaddingMaskChanged));

		private static void OnIsPaddingMaskChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
			=> VisibleBoundsDetails.GetInstance(dependencyObject as FrameworkElement).OnIsPaddingMaskChanged((PaddingMask)args.OldValue, (PaddingMask)args.NewValue);

		/// <summary>
		/// If false, ApplicationView.VisibleBounds and Window.Current.Bounds have different aspect ratios (eg portrait vs landscape) which 
		/// might arise transiently when the screen orientation changes.
		/// </summary>
		private static bool AreBoundsAspectRatiosConsistent => ApplicationView.GetForCurrentView().VisibleBounds.GetOrientation() == Window.Current.Bounds.GetOrientation();

		public class VisibleBoundsDetails
		{
			private static readonly ConditionalWeakTable<FrameworkElement, VisibleBoundsDetails> _instances =
				new ConditionalWeakTable<FrameworkElement, VisibleBoundsDetails>();
			private readonly WeakReference _owner;
			private readonly TypedEventHandler<global::Windows.UI.ViewManagement.ApplicationView, object> _visibleBoundsChanged;
			private PaddingMask _paddingMask;
			private readonly Thickness _originalPadding;

			internal VisibleBoundsDetails(FrameworkElement owner)
			{
				_owner = new WeakReference(owner);

				_originalPadding = owner.GetPadding();

				_visibleBoundsChanged = (s2, e2) => UpdatePadding();

#if __IOS__
				// For iOS, it's required to react on SizeChanged to prevent weird alignment
				// problems with Text using the LayoutManager (NSTextContainer).
				// https://github.com/unoplatform/uno/issues/2836
				owner.SizeChanged += (s, e) => UpdatePadding();
#endif
				owner.LayoutUpdated += (s, e) => UpdatePadding();

				owner.Loaded += (s, e) =>
				{
					UpdatePadding();
					ApplicationView.GetForCurrentView().VisibleBoundsChanged += _visibleBoundsChanged;
				};
				owner.Unloaded += (s, e) => ApplicationView.GetForCurrentView().VisibleBoundsChanged -= _visibleBoundsChanged;
			}

			private FrameworkElement Owner => _owner.Target as FrameworkElement;

			private void UpdatePadding()
			{
				if (Window.Current.Content == null)
				{
					return;
				}

				if (!AreBoundsAspectRatiosConsistent)
				{
					return;
				}

				if (!Owner.IsLoaded)
				{
					return;
				}

				Thickness visibilityPadding;
				
				if (WindowPadding.Left != 0
					|| WindowPadding.Right != 0
					|| WindowPadding.Top != 0
					|| WindowPadding.Bottom != 0)
				{
					var scrollAncestor = GetScrollAncestor();

					// If the owner view is scrollable, the visibility of interest is that of the scroll viewport.
					var fixedControl = scrollAncestor ?? Owner;

					var controlBounds = GetRelativeBounds(fixedControl, Window.Current.Content);

					visibilityPadding = CalculateVisibilityPadding(OffsetVisibleBounds, controlBounds);

					if (scrollAncestor != null)
					{
						visibilityPadding = AdjustScrollablePadding(visibilityPadding, scrollAncestor);
					}
				}
				else
				{
					visibilityPadding = default(Thickness);
				}

				var padding = CalculateAppliedPadding(_paddingMask, visibilityPadding);

				ApplyPadding(padding);
			}

			/// <summary>
			/// Calculate the padding required to keep the view entirely within the 'safe' visible bounds of the window.
			/// </summary>
			/// <param name="visibleBounds">The safe visible bounds of the window.</param>
			/// <param name="controlBounds">The bounds of the control, in the window's coordinates.</param>
			private Thickness CalculateVisibilityPadding(Rect visibleBounds, Rect controlBounds)
			{
				var windowPadding = WindowPadding;

				var left = Math.Min(visibleBounds.Left - controlBounds.Left, windowPadding.Left);
				var top = Math.Min(visibleBounds.Top - controlBounds.Top, windowPadding.Top);
				var right = Math.Min(controlBounds.Right - visibleBounds.Right, windowPadding.Right);
				var bottom = Math.Min(controlBounds.Bottom - visibleBounds.Bottom, windowPadding.Bottom);

				return new Thickness {
					Left = left,
					Top = top,
					Right = right,
					Bottom = bottom
				};
			}

			/// <summary>
			/// Apply adjustments when target view is inside of a ScrollViewer.
			/// </summary>
			private Thickness AdjustScrollablePadding(Thickness visibilityPadding, ScrollViewer scrollAncestor)
			{
				var scrollableRoot = scrollAncestor.Content as FrameworkElement;
#if XAMARIN
				if (scrollableRoot is ItemsPresenter)
				{
					// This implies we're probably inside a ListView, in which case the reasoning breaks down in Uno (because ItemsPresenter 
					// is *outside* the scrollable region); we skip the adjustment and hope for the best.
					scrollableRoot = null;
				}
#endif
				if (scrollableRoot != null)
				{
					// Get the spacing already provided by the alignment of the child relative to it ancestor at the root of the scrollable hierarchy.
					var controlBounds = GetRelativeBounds(Owner, scrollableRoot);
					var rootBounds = new Rect(0, 0, scrollableRoot.ActualWidth, scrollableRoot.ActualHeight);

					// Adjust for existing spacing
					visibilityPadding.Left -= controlBounds.Left - rootBounds.Left;
					visibilityPadding.Top -= controlBounds.Top - rootBounds.Top;
					visibilityPadding.Right -= rootBounds.Right - controlBounds.Right;
					visibilityPadding.Bottom -= rootBounds.Bottom - controlBounds.Bottom;
				}

				return visibilityPadding;
			}

			/// <summary>
			/// Calculate the padding to apply to the view, based on the selected PaddingMask.
			/// </summary>
			/// <param name="mask">The PaddingMask settings.</param>
			/// <param name="visibilityPadding">The padding required to keep the view entirely within the 'safe' visible bounds of the window.</param>
			/// <returns>The padding that will actually be set on the view.</returns>
			private Thickness CalculateAppliedPadding(PaddingMask mask, Thickness visibilityPadding)
			{
				// Apply left padding if the PaddingMask is "left" or "all"
				var left = mask.HasFlag(PaddingMask.Left)
					? Math.Max(_originalPadding.Left, visibilityPadding.Left)
					: _originalPadding.Left;
				// Apply top padding if the PaddingMask is "top" or "all"
				var top = mask.HasFlag(PaddingMask.Top)
					? Math.Max(_originalPadding.Top, visibilityPadding.Top)
					: _originalPadding.Top;
				// Apply right padding if the PaddingMask is "right" or "all"
				var right = mask.HasFlag(PaddingMask.Right)
					? Math.Max(_originalPadding.Right, visibilityPadding.Right)
					: _originalPadding.Right;
				// Apply bottom padding if the PaddingMask is "bottom" or "all"
				var bottom = mask.HasFlag(PaddingMask.Bottom)
					? Math.Max(_originalPadding.Bottom, visibilityPadding.Bottom)
					: _originalPadding.Bottom;

				return new Thickness {
					Left = left,
					Top = top,
					Right = right,
					Bottom = bottom
				};
			}

			private void ApplyPadding(Thickness padding)
			{
				if (Owner.SetPadding(padding) && _log.Value.IsEnabled(LogLevel.Debug))
				{
					_log.Value.LogDebug($"ApplyPadding={padding}");
				}
			}

			internal static VisibleBoundsDetails GetInstance(FrameworkElement element)
				=> _instances.GetValue(element, e => new VisibleBoundsDetails(e));

			internal void OnIsPaddingMaskChanged(PaddingMask oldValue, PaddingMask newValue)
			{
				_paddingMask = newValue;

				UpdatePadding();
			}

			private ScrollViewer GetScrollAncestor()
			{
				return Owner.FindFirstParent<ScrollViewer>();
			}

			private static Rect GetRelativeBounds(FrameworkElement boundsOf, UIElement relativeTo)
			{
				return boundsOf
					.TransformToVisual(relativeTo)
					.TransformBounds(new Rect(0, 0, boundsOf.ActualWidth, boundsOf.ActualHeight));
			}
		}
	}
}
