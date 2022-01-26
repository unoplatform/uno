using System;
using Uno.Helpers;
using Windows.Foundation;

namespace Windows.ApplicationModel;

/// <summary>
/// Gets the deferral object when the app is leaving the background state.
/// </summary>
public partial class LeavingBackgroundEventArgs : ILeavingBackgroundEventArgs
{
	internal LeavingBackgroundEventArgs(Action onComplete)
	{
		DeferralManager = new DeferralManager<Deferral>(c => new Deferral(c));
		DeferralManager.Completed += (s, e) => onComplete?.Invoke();
	}

	internal DeferralManager<Deferral> DeferralManager { get; }

	public Deferral GetDeferral() => DeferralManager.GetDeferral();
}
