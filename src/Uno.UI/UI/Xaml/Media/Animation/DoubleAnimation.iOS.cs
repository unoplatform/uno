using Uno.Extensions;
using Uno.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Windows.UI.Xaml.Media.Animation
{
	public partial class DoubleAnimation
	{
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
		partial void OnFrame()
		{
			if (!this.GetIsHardwareAnimated())
			{
				SetValue(_animator.AnimatedValue);
			}
			else
			{
				this.SetValueBypassPropagation(_animator.AnimatedValue);
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
				SetValue(ComputeToValue());
			}
		}

		private bool ReportEachFrame() => true;

		partial void DisposePartial()
		{
			_animator?.Dispose();
		}
	}
}
