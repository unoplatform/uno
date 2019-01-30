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

		private bool ReportEachFrame() => true;

		partial void DisposePartial()
		{
			_animator?.Dispose();
		}
	}
}
