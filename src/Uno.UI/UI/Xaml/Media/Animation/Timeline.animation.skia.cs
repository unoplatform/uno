using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Media.Animation
{
	partial class Timeline
	{
		partial class AnimationImplementation<T>
		{
			private bool ReportEachFrame() => true;
			partial void OnFrame()
			{
				SetValue(_animator.AnimatedValue);
			}
		}
	}
}
