#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Uno.UI.Extensions;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

#if __IOS__
using UIKit;
#elif __MACOS__
using AppKit;
#endif

#if HAS_UNO // Is building using Uno.UI
using Uno.Collections;
using Uno.Extensions;
using Uno.Foundation.Logging;
#endif

#if IS_UNO // Is inside the Uno.UI project
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
#if HAS_UNO // Is building using Uno.UI
#if IS_UNO
		private static readonly Logger _log = typeof(InternalVisibleBoundsPadding).Log();
#else
		private static readonly Logger _log = typeof(VisibleBoundsPadding).Log();
#endif
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
#if WINUI || HAS_UNO_WINUI
				return new();
#else
				var window =
#if HAS_UNO
					Windows.UI.Xaml.Window.CurrentSafe;
#else
					Windows.UI.Xaml.Window.Current;
#endif
				if (window is null)
				{
					return new();
				}

				var visibleBounds = ApplicationView.GetForCurrentView().VisibleBounds;
				var bounds = window.Bounds;
				var result = new Thickness
				{
					Left = visibleBounds.Left - bounds.Left,
					Top = visibleBounds.Top - bounds.Top,
					Right = bounds.Right - visibleBounds.Right,
					Bottom = bounds.Bottom - visibleBounds.Bottom
				};

#if HAS_UNO
				if (_log.IsEnabled(LogLevel.Debug))
				{
					_log.LogDebug($"WindowPadding={result} bounds={bounds} visibleBounds={visibleBounds}");
				}
#endif

				return result;
#endif
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

#pragma warning disable RS0030 // Do not used banned APIs, This can't be changed because it's built by UWP.
		public static DependencyProperty PaddingMaskProperty { get; } =
			DependencyProperty.RegisterAttached("PaddingMask", typeof(PaddingMask), typeof(_VisibleBoundsPadding), new PropertyMetadata(PaddingMask.None, OnIsPaddingMaskChanged));
#pragma warning restore RS0030 // Do not used banned APIs

		private static void OnIsPaddingMaskChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
#if WINUI
			// VisibleBoundsPadding is disabled for WinUI 3 and up as there's available API for bounds.
#else
			if (dependencyObject is FrameworkElement fe)
			{
				VisibleBoundsDetails.GetInstance(fe).OnIsPaddingMaskChanged((PaddingMask)args.OldValue, (PaddingMask)args.NewValue);
			}
			else
			{
#if HAS_UNO // Is building using Uno.UI
				if (dependencyObject.Log().IsEnabled(LogLevel.Debug))
				{
					dependencyObject.Log().LogDebug($"PaddingMask is only supported on FrameworkElement (Found {dependencyObject?.GetType()})");
				}
#endif
			}
#endif
		}

#if !WINUI
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

			private FrameworkElement? Owner => _owner.Target as FrameworkElement;

			/// <summary>
			/// VisibleBounds offset to the reference frame of the window Bounds.
			/// </summary>
			private Rect OffsetVisibleBounds
			{
				get
				{
					Rect visibleBounds = default;
					if (GetOwnerWindow() is Window window)
					{
						visibleBounds = GetApplicationView(window).VisibleBounds;
						var bounds = window.Bounds;
						visibleBounds.X -= bounds.X;
						visibleBounds.Y -= bounds.Y;
					}

					return visibleBounds;
				}
			}

			private ApplicationView GetApplicationView(Window window)
			{
#if HAS_UNO
				return ApplicationView.GetForWindowId(window.AppWindow.Id);
#else
				return ApplicationView.GetForCurrentView();
#endif
			}

			private Windows.UI.Xaml.Window? GetOwnerWindow()
			{
#if HAS_UNO
				return Owner?.XamlRoot?.HostWindow ?? Windows.UI.Xaml.Window.CurrentSafe;
#else
				return Windows.UI.Xaml.Window.Current;
#endif
			}

			private void UpdatePadding()
			{
				var window = GetOwnerWindow();

				if (window?.Content == null)
				{
					return;
				}

#if HAS_UNO
				var areBoundsConsistent =
					window.Bounds.GetOrientation() == ApplicationView.GetForWindowId(window.AppWindow.Id).VisibleBounds.GetOrientation();
#else
				var areBoundsConsistent =
					ApplicationView.GetForCurrentView().VisibleBounds.GetOrientation() == GetOwnerWindow()?.Bounds.GetOrientation();
#endif
				// If false, ApplicationView.VisibleBounds and Window.Current.Bounds have different aspect ratios (eg portrait vs landscape) which
				// might arise transiently when the screen orientation changes.
				if (!areBoundsConsistent)
				{
					return;
				}

				if (Owner is null || !Owner.IsLoaded)
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

					// Using relativeTo: null instead of Window.Current.Content since there are cases when the current UIElement
					// may be outside the bounds of the current Window content, for example, when the element is hosted in a modal window.
					var controlBounds = GetRelativeBounds(fixedControl, relativeTo: null);

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

				return new Thickness
				{
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
				if (scrollableRoot != null && Owner is { })
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

				return new Thickness
				{
					Left = left,
					Top = top,
					Right = right,
					Bottom = bottom
				};
			}

			private void ApplyPadding(Thickness padding)
			{
#if HAS_UNO // Is building using Uno.UI
				if (Owner is { } owner &&
					!owner.GetPadding().Equals(padding) &&
					owner.SetPadding(padding))
				{
					if (_log.IsEnabled(LogLevel.Debug))
					{
						_log.Log(LogLevel.Debug, $"ApplyPadding={padding}");
					}

#if __ANDROID__
					// Dispatching on Android prevents issues where layout/render changes occurring
					// during initial loading of the view are not always properly picked up by the layouting/rendering engine.
					_ = owner.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, owner.InvalidateMeasure);
#endif
				}
#endif
			}

			internal static VisibleBoundsDetails GetInstance(FrameworkElement element)
				=> _instances.GetValue(element, e => new VisibleBoundsDetails(e));

			internal void OnIsPaddingMaskChanged(PaddingMask oldValue, PaddingMask newValue)
			{
				_paddingMask = newValue;

				UpdatePadding();
			}

			private ScrollViewer? GetScrollAncestor()
			{
				return Owner?.FindFirstParent<ScrollViewer>();
			}

			private static Rect GetRelativeBounds(FrameworkElement boundsOf, UIElement? relativeTo)
			{
				return boundsOf
					.TransformToVisual(relativeTo)
					.TransformBounds(new Rect(0, 0, boundsOf.ActualWidth, boundsOf.ActualHeight));
			}
		}
#endif
	}
}
