#if UNO_HAS_MANAGED_SCROLL_PRESENTER
using System.Diagnostics;
using Windows.UI.Xaml.Input;
using Windows.Devices.Input;
using Windows.Foundation;

#if HAS_UNO_WINUI
using _PointerDeviceType = global::Microsoft.UI.Input.PointerDeviceType;
#else
using _PointerDeviceType = global::Windows.Devices.Input.PointerDeviceType;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class ScrollContentPresenter : ContentPresenter
#if !__CROSSRUNTIME__ && !IS_UNIT_TESTS
		, ICustomClippingElement
#endif
	{
		private /*readonly - partial*/ IScrollStrategy _strategy;

		private bool _canHorizontallyScroll;
		public bool CanHorizontallyScroll
		{
			get => _canHorizontallyScroll
#if __SKIA__
			|| _forceChangeToCurrentView
#endif
			;
			set => _canHorizontallyScroll = value;
		}

		private bool _canVerticallyScroll;
		public bool CanVerticallyScroll
		{
			get => _canVerticallyScroll
#if __SKIA__
			|| _forceChangeToCurrentView
#endif
			;
			set => _canVerticallyScroll = value;
		}

		public double HorizontalOffset { get; private set; }

		public double VerticalOffset { get; private set; }

		public double ExtentHeight { get; internal set; }

		public double ExtentWidth { get; internal set; }

		internal Size ScrollBarSize => new Size(0, 0);

		internal Size? CustomContentExtent => null;

		private object RealContent => Content;

		partial void InitializePartial()
		{
#if __SKIA__
			_strategy = CompositorScrollStrategy.Instance;
#elif __MACOS__
			_strategy = TransformScrollStrategy.Instance;
#endif

			_strategy.Initialize(this);

			// Mouse wheel support
			PointerWheelChanged += PointerWheelScroll;

			// Touch scroll support
			ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY; // Updated in PrepareTouchScroll!
			ManipulationStarting += PrepareTouchScroll;
			ManipulationStarted += TouchScrollStarted;
			ManipulationDelta += UpdateTouchScroll;
			ManipulationCompleted += CompleteTouchScroll;
		}

		/// <inheritdoc />
		protected override void OnContentChanged(object oldValue, object newValue)
		{
			if (oldValue is UIElement oldElt)
			{
				_strategy.Update(oldElt, 0, 0, 1, disableAnimation: true);
			}

			base.OnContentChanged(oldValue, newValue);

			if (newValue is UIElement newElt)
			{
				_strategy.Update(newElt, HorizontalOffset, VerticalOffset, 1, disableAnimation: true);
			}
		}

		internal void OnMinZoomFactorChanged(float newValue) { }

		internal void OnMaxZoomFactorChanged(float newValue) { }

		internal bool Set(
			double? horizontalOffset = null,
			double? verticalOffset = null,
			float? zoomFactor = null,
			bool disableAnimation = false,
			bool isIntermediate = false)
		{
			var success = true;

			if (horizontalOffset is double hOffset)
			{
				var maxOffset = Scroller?.ScrollableWidth ?? ExtentWidth - ViewportWidth;

				var scrollX = ValidateInputOffset(hOffset, 0, maxOffset);

				success &= scrollX == hOffset;

				if (!NumericExtensions.AreClose(HorizontalOffset, scrollX))
				{
					HorizontalOffset = scrollX;
				}
			}

			if (verticalOffset is double vOffset)
			{
				var maxOffset = Scroller?.ScrollableHeight ?? ExtentHeight - ViewportHeight;
				var scrollY = ValidateInputOffset(vOffset, 0, maxOffset);

				success &= scrollY == vOffset;

				if (!NumericExtensions.AreClose(VerticalOffset, scrollY))
				{
					VerticalOffset = scrollY;
				}
			}

			Apply(disableAnimation, isIntermediate);

			return success;
		}

		private void Apply(bool disableAnimation, bool isIntermediate)
		{
			if (Content is UIElement contentElt)
			{
				_strategy.Update(contentElt, HorizontalOffset, VerticalOffset, 1, disableAnimation);
			}

			Scroller?.OnPresenterScrolled(HorizontalOffset, VerticalOffset, isIntermediate);

			// Note: We do not capture the offset so if they are altered in the OnPresenterScrolled,
			//		 we will apply only the final ScrollOffsets and only once.
			ScrollOffsets = new Point(HorizontalOffset, VerticalOffset);
			InvalidateViewport();
		}

		private void PrepareTouchScroll(object sender, ManipulationStartingRoutedEventArgs e)
		{
			if (e.Container != this)
			{
				// This gesture is coming from a nested element, we just ignore it!
				return;
			}

			if (e.Pointer.Type != PointerDeviceType.Touch)
			{
				e.Mode = ManipulationModes.None;
				return;
			}

			if (!CanVerticallyScroll || ExtentHeight <= 0)
			{
				e.Mode &= ~ManipulationModes.TranslateY;
			}

			if (!CanHorizontallyScroll || ExtentWidth <= 0)
			{
				e.Mode &= ~ManipulationModes.TranslateX;
			}
		}

		private void TouchScrollStarted(object sender, ManipulationStartedRoutedEventArgs e)
		{
			if (e.Container != this)
			{
				// This gesture is coming from a nested element, we just ignore it!
				return;
			}

			if (e.PointerDeviceType == _PointerDeviceType.Touch)
			{
				Debug.Assert(PointerRoutedEventArgs.LastPointerEvent.Pointer.UniqueId == e.Pointers[0]);
				this.CapturePointer(PointerRoutedEventArgs.LastPointerEvent.Pointer);
			}
		}

		private void UpdateTouchScroll(object sender, ManipulationDeltaRoutedEventArgs e)
		{
			if (e.Container != this) // No needs to check the pointer type, if the manip is local it's touch, otherwise it was cancelled in starting.
			{
				// This gesture is coming from a nested element, we just ignore it!
				return;
			}

			Set(
				horizontalOffset: HorizontalOffset - e.Delta.Translation.X,
				verticalOffset: VerticalOffset - e.Delta.Translation.Y,
				disableAnimation: true,
				isIntermediate: true);
		}

		private void CompleteTouchScroll(object sender, ManipulationCompletedRoutedEventArgs e)
		{
			if (e.Container != this || (PointerDeviceType)e.PointerDeviceType != PointerDeviceType.Touch)
			{
				return;
			}

			Set(disableAnimation: true, isIntermediate: false);

			Debug.Assert(PointerRoutedEventArgs.LastPointerEvent.Pointer.UniqueId == e.Pointers[0]);
			this.ReleasePointerCapture(PointerRoutedEventArgs.LastPointerEvent.Pointer);
		}

#if !__CROSSRUNTIME__ && !IS_UNIT_TESTS
		bool ICustomClippingElement.AllowClippingToLayoutSlot => true;
		bool ICustomClippingElement.ForceClippingToLayoutSlot => true; // force scrollviewer to always clip
#endif
	}
}
#endif
