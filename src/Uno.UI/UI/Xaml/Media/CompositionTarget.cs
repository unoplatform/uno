using System;
using System.Collections.Generic;
using System.Text;
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
