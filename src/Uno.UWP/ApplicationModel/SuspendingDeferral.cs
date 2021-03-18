#nullable enable

using System;
using System.Diagnostics;
using Windows.Foundation;

namespace Windows.ApplicationModel
{
	public sealed partial class SuspendingDeferral : ISuspendingDeferral
	{
		private readonly Action? _deferralDone;
		private readonly DeferralCompletedHandler? _handler;

		/// <summary>
		/// This can be removed with other breaking changes
		/// </summary>
		/// <param name="deferralDone"></param>
		[DebuggerHidden]
		public SuspendingDeferral(Action? deferralDone)	=>
			_deferralDone = deferralDone;

		internal SuspendingDeferral(DeferralCompletedHandler handler) =>
			_handler = handler;

		public void Complete()
		{
			_deferralDone?.Invoke();
			_handler?.Invoke();
		}
	}
}
