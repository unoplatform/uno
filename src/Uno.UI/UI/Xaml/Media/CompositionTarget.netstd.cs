using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI;
using Windows.UI.Core;

namespace Microsoft.UI.Xaml.Media
{
	public partial class CompositionTarget
	{
		public static event EventHandler<object> Rendering
		{
			add
			{
				CoreDispatcher.CheckThreadAccess();

				var currentlyRaisingEvents = Uno.UI.Dispatching.CoreDispatcher.Main.ShouldRaiseRenderEvents;
				Uno.UI.Dispatching.CoreDispatcher.Main.Rendering += value;
				Uno.UI.Dispatching.CoreDispatcher.Main.RenderEventThrottle = FeatureConfiguration.CompositionTarget.RenderEventThrottle;
				Uno.UI.Dispatching.CoreDispatcher.Main.RenderingEventArgsGenerator ??= (d => new RenderingEventArgs(d));
				if (!currentlyRaisingEvents)
				{
					Uno.UI.Dispatching.CoreDispatcher.Main.WakeUp();
				}
			}
			remove
			{
				Uno.UI.Dispatching.CoreDispatcher.CheckThreadAccess();

				Uno.UI.Dispatching.CoreDispatcher.Main.Rendering -= value;
			}
		}
	}
}
