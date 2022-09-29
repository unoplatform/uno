using System;
using Uno.Helpers;
using Windows.Foundation;

namespace Windows.ApplicationModel;

/// <summary>
/// Gets the deferral object when the app is leaving the background state.
/// </summary>
public partial class LeavingBackgroundEventArgs : ILeavingBackgroundEventArgs
{
	internal LeavingBackgroundEventArgs(Action? onComplete)
	{
		DeferralManager = new DeferralManager<Deferral>(c => new Deferral(c));
		DeferralManager.Completed += (s, e) => onComplete?.Invoke();
	}

	internal DeferralManager<Deferral> DeferralManager { get; }

	/// <summary>
	/// Gets the deferral object which delays the transition from running
	/// in the background to running in the foreground until the app calls
	/// Deferral.Complete or the deadline for navigation has passed.
	/// </summary>
	/// <returns>The deferral object you will use to indicate that your processing is done.</returns>
	public Deferral GetDeferral() => DeferralManager.GetDeferral();
}
