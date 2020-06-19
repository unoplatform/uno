#nullable enable

using System;
using Uno;
using Windows.Foundation;
using Windows.Foundation.Metadata;
namespace Windows.ApplicationModel
{
	public sealed partial class SuspendingDeferral
	{
		private Action? _deferralDone;

		public SuspendingDeferral(Action? deferralDone)
			=> _deferralDone = deferralDone;

		public void Complete()
			=> _deferralDone?.Invoke();
	}
}
