#nullable enable

#if HAS_UNO_WINUI
using Windows.ApplicationModel.Activation;

namespace Microsoft.UI.Xaml
{
	public sealed partial class LaunchActivatedEventArgs
	{
		internal LaunchActivatedEventArgs() : this(ActivationKind.Launch, null)
		{
		}

		internal LaunchActivatedEventArgs(ActivationKind kind, string? arguments)
		{
			Arguments = arguments;
			UWPLaunchActivatedEventArgs = new global::Windows.ApplicationModel.Activation.LaunchActivatedEventArgs(kind, arguments);
		}

		internal LaunchActivatedEventArgs(global::Windows.ApplicationModel.Activation.LaunchActivatedEventArgs uwpLaunchActivatedEventArgs)
		{
			UWPLaunchActivatedEventArgs = uwpLaunchActivatedEventArgs;
		}

		public string? Arguments { get; }

		public global::Windows.ApplicationModel.Activation.LaunchActivatedEventArgs UWPLaunchActivatedEventArgs { get; }
	}
}
#endif
