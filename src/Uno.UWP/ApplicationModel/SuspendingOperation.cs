#nullable enable

using System;
using Uno.Helpers;

namespace Windows.ApplicationModel
{
	public sealed partial class SuspendingOperation : ISuspendingOperation
	{
		private DeferralManager<SuspendingDeferral>? _deferralManager;
		private readonly Action? _deferralDone;

		internal SuspendingOperation(DateTimeOffset offset, Action? deferralDone = null)
		{
			Deadline = offset;
			_deferralDone = deferralDone;
		}

		internal bool IsDeferred => _deferralManager != null;

		internal void EventRaiseCompleted() => _deferralManager?.EventRaiseCompleted();

		public DateTimeOffset Deadline { get; }

		public SuspendingDeferral GetDeferral()
		{
			if (_deferralManager == null)
			{
				_deferralManager = new DeferralManager<SuspendingDeferral>(h => new SuspendingDeferral(h));
				_deferralManager.Completed += (s, e) => _deferralDone?.Invoke();
			}
			return _deferralManager.GetDeferral();
		}		
	}
}
