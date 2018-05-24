using System;
using Windows.Foundation;
using Windows.Foundation.Metadata;
namespace Windows.ApplicationModel
{
	public sealed partial class SuspendingEventArgs
	{
		internal SuspendingEventArgs(SuspendingOperation operation)
		{
			SuspendingOperation = operation;
		}

		public SuspendingOperation SuspendingOperation { get; }
	}
}
