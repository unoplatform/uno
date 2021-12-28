using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.UI.Core
{
	public sealed partial class CoreDispatcher
	{
		public static bool HasThreadAccessOverride
		{
			get => Uno.UI.Dispatching.CoreDispatcher.HasThreadAccessOverride;
			set => Uno.UI.Dispatching.CoreDispatcher.HasThreadAccessOverride = value;
		}

		public void ProcessEvents(CoreProcessEventsOption options)
			=> _inner.ProcessEvents((Uno.UI.Dispatching.CoreProcessEventsOption)options);
	}
}
