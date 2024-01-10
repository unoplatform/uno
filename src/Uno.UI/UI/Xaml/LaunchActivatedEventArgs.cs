#nullable enable

#if HAS_UNO_WINUI
using Windows.ApplicationModel.Activation;

namespace Microsoft/* UWP don't rename */.UI.Xaml
{
	public sealed partial class LaunchActivatedEventArgs
	{
		internal LaunchActivatedEventArgs() : this(ActivationKind.Launch, null)
		{
		}

		internal LaunchActivatedEventArgs(ActivationKind kind, string? arguments)
		{
			Arguments = arguments;
			UWPLaunchActivatedEventArgs = new Windows.ApplicationModel.Activation.LaunchActivatedEventArgs(kind, arguments);
		}

		public string? Arguments { get; }

		public Windows.ApplicationModel.Activation.LaunchActivatedEventArgs UWPLaunchActivatedEventArgs { get; }
	}
}
#endif
