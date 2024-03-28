using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Media.Animation
{
	public partial class DoubleAnimationUsingKeyFrames
	{
		// TODO: Don't return true
		private bool ReportEachFrame() => true;

		/// <summary>
		/// Ensures that the animation is running on the GPU 
		/// </summary>
		partial void UseHardware()
		{
			//Do nothing 
			//Already runs on gpu
		}

		/// <summary>
		/// Set the value on a new frame
		/// </summary>
		partial void OnFrame(IValueAnimator currentAnimator)
		{
			if (!this.GetIsHardwareAnimated() || currentAnimator is DiscreteFloatValueAnimator)
			{
				SetValue(currentAnimator.AnimatedValue);
			}
			else
			{
				this.SetValueBypassPropagation(currentAnimator.AnimatedValue);
			}
		}

		partial void HoldValue()
		{
			// If we have a GPU based animation, it means that the animated values were correctly set
			// but never used to update the underlying property.  We hence need to set it explicitly
			// at this point so that it can be held
			if (this.GetIsHardwareAnimated())
			{
				// ClearValue in order to make sure the value is considered to be changed
				ClearValue();
				SetValue(_finalValue);
			}
		}
	}
}
