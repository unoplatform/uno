#nullable enable

using System;
using Windows.Foundation;
using Windows.Foundation.Metadata;
namespace Windows.ApplicationModel
{
	public sealed partial class SuspendingOperation
	{
		private readonly Action? _deferralDone;

		internal SuspendingOperation(DateTimeOffset offset, Action? deferralDone = null)
		{
			Deadline = offset;
			_deferralDone = deferralDone;
		}

		public DateTimeOffset Deadline { get; }

		public SuspendingDeferral GetDeferral()
			=> new SuspendingDeferral(_deferralDone);
	}
}
