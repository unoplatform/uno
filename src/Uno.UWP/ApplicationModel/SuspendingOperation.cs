using System;
using Windows.Foundation;
using Windows.Foundation.Metadata;
namespace Windows.ApplicationModel
{
	public sealed partial class SuspendingOperation
	{
		public SuspendingOperation(DateTimeOffset offset)
		{
		}

		[global::Uno.NotImplemented]
		public DateTimeOffset Deadline 
			=> DateTimeOffset.Now;

		[global::Uno.NotImplemented]
		public global::Windows.ApplicationModel.SuspendingDeferral GetDeferral()
			=> new SuspendingDeferral();
	}
}
