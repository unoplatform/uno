using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Windows.ApplicationModel.Activation
{
	public partial interface IActivatedEventArgs
	{
		ActivationKind Kind { get; }

		ApplicationExecutionState PreviousExecutionState { get; }

		SplashScreen? SplashScreen { get; }
	}
}
