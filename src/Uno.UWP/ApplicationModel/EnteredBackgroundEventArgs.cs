#nullable enable

using System;
using Uno.Helpers;
using Windows.Foundation;

namespace Windows.ApplicationModel;

/// <summary>
/// Gets the deferral object when an app has entered the background state.
/// </summary>
public partial class EnteredBackgroundEventArgs : IEnteredBackgroundEventArgs
{
	internal EnteredBackgroundEventArgs(Action? onComplete)
	{
		DeferralManager = new DeferralManager<Deferral>(c => new Deferral(c));
		DeferralManager.Completed += (s, e) => onComplete?.Invoke();
	}

	internal DeferralManager<Deferral> DeferralManager { get; }

	/// <summary>
	/// Gets the deferral object which delays the transition from running in the background
	/// state to the suspended state until the app calls Deferral.Complete or the deadline
	/// for navigation has passed.
	/// </summary>
	/// <returns>The deferral object you will use to indicate when your processing is complete.</returns>
	public Deferral GetDeferral() => DeferralManager.GetDeferral();
}
