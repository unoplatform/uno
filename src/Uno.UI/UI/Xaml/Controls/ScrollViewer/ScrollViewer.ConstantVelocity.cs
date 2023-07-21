#if IS_UNIT_TESTS
#pragma warning disable CS0067 // This event is never used
#endif

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using System.Text;
using Windows.Devices.Input;
using Uno.UI;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml.Input;
using Uno;
using Uno.Extensions;


#if __ANDROID__
using View = Android.Views.View;
using Font = Android.Graphics.Typeface;
#elif __IOS__
using UIKit;
using View = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
#elif __MACOS__
using View = AppKit.NSView;
using AppKit;
#else
using View = Windows.UI.Xaml.UIElement;
#endif

#if UNO_HAS_MANAGED_SCROLL_PRESENTER
using _ScrollContentPresenter = Windows.UI.Xaml.Controls.ScrollContentPresenter;
#else
using _ScrollContentPresenter = Windows.UI.Xaml.Controls.IScrollContentPresenter;
#endif

namespace Windows.UI.Xaml.Controls
{
	partial class ScrollViewer
	{

		private ConstantVelocityScroller? _constantVelocityScroller;
		internal void SetConstantVelocities(float horizontalVelocity, float verticalVelocity)
		{
			_constantVelocityScroller ??= new ConstantVelocityScroller(this);
			// Flip sign here for consistency with imported WinUI code
			_constantVelocityScroller.HorizontalVelocity = -horizontalVelocity;
			_constantVelocityScroller.VerticalVelocity = -verticalVelocity;
		}

		/// <summary>
		/// Apply a constant scroll velocity, used as a substitute for constant-pan Manipulation functionality which Uno doesn't currently support.
		/// </summary>
		private class ConstantVelocityScroller
		{
			private readonly DispatcherTimer _timer = new DispatcherTimer();
			private readonly ScrollViewer parent;
			private bool _isStarted;
			private long? _previousTick;

			private float _horizontalVelocity;
			public float HorizontalVelocity
			{
				get => _horizontalVelocity;
				set
				{
					_horizontalVelocity = value;
					StartOrStopTimer();
				}
			}

			private float _verticalVelocity;
			public float VerticalVelocity
			{
				get => _verticalVelocity;
				set
				{
					_verticalVelocity = value;
					StartOrStopTimer();
				}
			}

			/// <summary>
			/// Custom value. The exact value is not too important here, the idea is just to update the scroll frequently, but still give the
			/// layouting enough time to update.
			/// </summary>
			private const int FrameIntervalMS = 1000 / 40;

			public ConstantVelocityScroller(ScrollViewer _parent)
			{
				this.parent = _parent;
				_timer.Tick += UpdateScrollPosition;
				_timer.Interval = TimeSpan.FromMilliseconds(FrameIntervalMS);
			}

			private void StartOrStopTimer()
			{
				var shouldStart = HorizontalVelocity != 0 || VerticalVelocity != 0;
				if (shouldStart && !_isStarted)
				{
					_timer.Start();
				}
				else if (!shouldStart && _isStarted)
				{
					_previousTick = null;
					_timer.Stop();
				}

				_isStarted = shouldStart;
			}

			private void UpdateScrollPosition(object? sender, object e)
			{
				var currentTick = DateTimeOffset.Now.Ticks;
				if (!(_previousTick is { } previousTick))
				{
					// Constant-velocity scroll has just started, set current time so we can calculate time delta on the next update.
					_previousTick = currentTick;
					return;
				}

				var timeElapsedS = (double)(currentTick - previousTick) / TimeSpan.TicksPerSecond;
				var horizontalDelta = HorizontalVelocity != 0 ?
					HorizontalVelocity * timeElapsedS :
					default(double?);
				var verticalDelta = VerticalVelocity != 0 ?
					VerticalVelocity * timeElapsedS :
					default(double?);

				if (horizontalDelta >= 1 || verticalDelta >= 1 || horizontalDelta <= 1 || verticalDelta <= 1)
				{
					// Clamp target offset to be >=0, to simplify layout calculations for dragging ListView
					parent.ChangeView(MathEx.Max(0, parent.HorizontalOffset + horizontalDelta), MathEx.Max(0, parent.VerticalOffset + verticalDelta), zoomFactor: null, disableAnimation: true);
					_previousTick = currentTick;
				}
				// else wait until a larger delta accumulates
			}
		}

	}
}
