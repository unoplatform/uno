#nullable enable

using Windows.Foundation;

namespace Windows.ApplicationModel;

/// <summary>
/// Gets the deferral object when an app has entered the background state.
/// </summary>
public partial interface IEnteredBackgroundEventArgs
{
	/// <summary>
	/// Gets the deferral object which delays the transition
	/// from running in the background state to the suspended
	/// state until the app calls Deferral.Complete or the deadline
	/// for navigation has passed.
	/// </summary>
	/// <returns>
	/// The deferral object you will use to indicate
	/// that your processing is complete.
	/// </returns>
	Deferral GetDeferral();
}
