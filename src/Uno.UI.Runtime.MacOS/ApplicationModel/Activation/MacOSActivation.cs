using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Uno.Foundation.Logging;
using Windows.ApplicationModel.Activation;

namespace Uno.UI.Runtime.MacOS;

/// <summary>
/// Reports the URLs LaunchServices hands to the app delegate as protocol activations on <see cref="AppInstance"/>.
/// </summary>
/// <remarks>
/// On a cold start AppKit delivers <c>application:openURLs:</c> before <c>applicationDidFinishLaunching:</c>,
/// so before <see cref="Application.Start"/> has run. <see cref="AppInstance"/> does not depend on
/// <see cref="Application"/>, so the activation is simply stored and read back in <c>OnLaunched</c>.
/// </remarks>
internal static class MacOSActivation
{
	public static unsafe void Register() => NativeUno.uno_set_application_open_url_callback(&OnOpenUrl);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static unsafe void OnOpenUrl(sbyte* url)
	{
		// A managed exception unwinding into AppKit terminates the process, and this runs app Activated handlers.
		try
		{
			TryReportUrl(Marshal.PtrToStringUTF8((IntPtr)url));
		}
		catch (Exception ex)
		{
			Application.Current?.RaiseRecoverableUnhandledException(ex);
		}
	}

	internal static bool TryReportUrl(string? url)
	{
		if (string.IsNullOrEmpty(url))
		{
			return false;
		}

		if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
		{
			// Scheme only: an activation URI routinely carries an OAuth code or token.
			var schemeEnd = url.IndexOf(':');
			typeof(MacOSActivation).Log().LogError(
				$"An activation URL with scheme '{(schemeEnd > 0 ? url[..schemeEnd] : "(none)")}' could not be parsed as an absolute URI.");
			return false;
		}

		var instance = AppInstance.GetCurrent();
		var previousExecutionState = instance.HasLaunched ? ApplicationExecutionState.Running : ApplicationExecutionState.NotRunning;

		instance.SetOrRaiseActivation(AppActivationArguments.CreateProtocol(new ProtocolActivatedEventArgs(uri, previousExecutionState)));
		return true;
	}
}
