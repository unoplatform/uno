using System;

using Uno.UI;
using Uno.UI.Dispatching;

namespace Windows.UI.Xaml.Media
{
	public partial class CompositionTarget
	{
		public static event EventHandler<object> Rendering
		{
			add
			{
				NativeDispatcher.CheckThreadAccess();

				var currentlyRaisingEvents = NativeDispatcher.Main.IsRendering;
				NativeDispatcher.Main.Rendering += value;
				NativeDispatcher.Main.RenderingEventThrottle = FeatureConfiguration.CompositionTarget.RenderEventThrottle;
				NativeDispatcher.Main.RenderingEventArgsGenerator ??= (d => new RenderingEventArgs(d));

				if (!currentlyRaisingEvents)
				{
					NativeDispatcher.Main.WakeUp();
				}
			}
			remove
			{
				NativeDispatcher.CheckThreadAccess();

				NativeDispatcher.Main.Rendering -= value;
			}
		}
	}
}
