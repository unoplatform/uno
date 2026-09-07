using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Windows.ApplicationModel.Activation;

/// <summary>
/// Provides common properties for all activation types.
/// </summary>
public partial interface IActivatedEventArgs
{
	/// <summary>
	/// Gets the reason that this app is being activated.
	/// </summary>
	ActivationKind Kind { get; }

	/// <summary>
	/// Gets the execution state of the app before this activation.
	/// </summary>
	ApplicationExecutionState PreviousExecutionState { get; }

	/// <summary>
	/// Gets the splash screen object that provides information about 
	/// the transition from the splash screen to the activated app.
	/// </summary>
	SplashScreen SplashScreen { get; }
}
