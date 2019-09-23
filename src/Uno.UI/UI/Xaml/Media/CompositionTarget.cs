using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI;
using Windows.UI.Core;

namespace Windows.UI.Xaml.Media
{
	public partial class CompositionTarget
	{
		public static event EventHandler<object> Rendering
		{
			add
			{
				CoreDispatcher.CheckThreadAccess();

				var currentlyRaisingEvents = CoreDispatcher.Main.ShouldRaiseRenderEvents;
				CoreDispatcher.Main.Rendering += value;
				CoreDispatcher.Main.RenderEventThrottle = FeatureConfiguration.CompositionTarget.RenderEventThrottle;
				CoreDispatcher.Main.RenderingEventArgsGenerator = CoreDispatcher.Main.RenderingEventArgsGenerator
					?? (d => new RenderingEventArgs(d));
				if (!currentlyRaisingEvents)
				{
					CoreDispatcher.Main.WakeUp();
				}
			}
			remove
			{
				CoreDispatcher.CheckThreadAccess();

				CoreDispatcher.Main.Rendering -= value;
			}
		}
	}
}
