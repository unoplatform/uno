using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.UI.Core
{
	public sealed partial class CoreDispatcher
	{
		private bool GetHasThreadAccess() => throw new NotSupportedException("Ref assembly");

		public static CoreDispatcher Main { get; } = new CoreDispatcher();

		internal bool IsQueueEmpty => _queues.All(q => q.Count == 0);
	}
}
