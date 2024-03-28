using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Media
{
	public sealed partial class RenderingEventArgs
	{
		internal RenderingEventArgs(TimeSpan renderingTime)
		{
			RenderingTime = renderingTime;
		}

		public TimeSpan RenderingTime { get; }
	}
}
