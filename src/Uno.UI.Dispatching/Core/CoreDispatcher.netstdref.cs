using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.Dispatching
{
	internal sealed partial class CoreDispatcher
	{
		private bool GetHasThreadAccess() => throw new NotSupportedException("Ref assembly");

		internal bool IsQueueEmpty => _queues.All(q => q.Count == 0);
	}
}
