using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Input;
using Foundation;
using UIKit;

namespace Windows.UI.Xaml.Controls
{
    public partial class Slider
	{
		private SliderGestureRecognizer _sliderGestureRecognizer;

		partial void RegisterNativeHandlers()
		{
			if (HasXamlTemplate && IsTrackerEnabled)
			{
				_sliderGestureRecognizer = new SliderGestureRecognizer(this);
				_sliderContainer.AddGestureRecognizer(_sliderGestureRecognizer);
			}
		}

		partial void UnregisterNativeHandlers()
		{
			if (HasXamlTemplate && _sliderGestureRecognizer != null)
			{
				_sliderContainer.RemoveGestureRecognizer(_sliderGestureRecognizer);
				_sliderGestureRecognizer.Dispose();
				_sliderGestureRecognizer = null; // Get rid of the possible circular reference
			}
		}

		/// <summary>
		/// This GestureRecognizer is used to eat the touch events sent to the slider. Without such a system,
		/// the events can be captured by a ScrollView if wrapped around a Slider.
		/// 
		/// This GestureRecognizer forwards the Touches events to the relevant events listened by SliderContainer
		/// while preventing capture by other views.
		/// </summary>
		private class SliderGestureRecognizer : UIGestureRecognizer
		{
			private Slider _slider;

			public SliderGestureRecognizer(Slider slider)
			{
				this._slider = slider;
			}

			public override void TouchesBegan(NSSet touches, UIEvent evt)
			{
				try
				{
					base.TouchesBegan(touches, evt);
					State = UIGestureRecognizerState.Began;

					this._slider.OnSliderContainerPressed(_slider._sliderContainer, new PointerRoutedEventArgs(touches, evt));
				}
				catch (Exception e)
				{
					Application.Current.RaiseRecoverableUnhandledException(e);
				}
			}

			public override void TouchesMoved(NSSet touches, UIEvent evt)
			{
				try
				{
					base.TouchesMoved(touches, evt);
					State = UIGestureRecognizerState.Changed;

					this._slider.OnSliderContainerMoved(_slider._sliderContainer, new PointerRoutedEventArgs(touches, evt));
				}
				catch (Exception e)
				{
					Application.Current.RaiseRecoverableUnhandledException(e);
				}
			}

			public override void TouchesEnded(NSSet touches, UIEvent evt)
			{
				try
				{
					base.TouchesEnded(touches, evt);
					State = UIGestureRecognizerState.Ended;

					this._slider.OnSliderContainerReleased(_slider._sliderContainer, new PointerRoutedEventArgs(touches, evt));
				}
				catch (Exception e)
				{
					Application.Current.RaiseRecoverableUnhandledException(e);
				}
			}

			public override void TouchesCancelled(NSSet touches, UIEvent evt)
			{
				try
				{ 
					base.TouchesCancelled(touches, evt);
					State = UIGestureRecognizerState.Cancelled;

					this._slider.OnSliderContainerCanceled(_slider._sliderContainer, new PointerRoutedEventArgs(touches, evt));
				}
				catch (Exception e)
				{
					Application.Current.RaiseRecoverableUnhandledException(e);
				}
			}
		}
	}
}
