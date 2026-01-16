#nullable enable

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

		public string? Arguments { get; }

		public global::Windows.ApplicationModel.Activation.LaunchActivatedEventArgs UWPLaunchActivatedEventArgs { get; }
	}
}
