#nullable enable

using System;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Windows.ApplicationModel.Activation;

namespace Uno.UI.Hosting;

/// <summary>
/// Reports a protocol activation carried on the command line, for desktop hosts where the OS
/// starts the app with the activation URI as an argument.
/// </summary>
internal static class CommandLineActivation
{
	/// <summary>
	/// Classifies the process arguments and stores the activation on <see cref="AppInstance"/>.
	/// Must run before <see cref="Application.Start"/> so <c>OnLaunched</c> can read it back.
	/// </summary>
	/// <param name="allowBareUri">Whether a lone URI argument counts as an activation, see <see cref="CommandLineActivationParser.TryParse"/>.</param>
	internal static void Report(bool allowBareUri)
	{
		var arguments = Environment.GetCommandLineArgs();
		if (arguments.Length <= 1)
		{
			return;
		}

		var found = CommandLineActivationParser.TryParse(arguments[1..], allowBareUri, out var uri, out var remainingArguments);

		// The activation token is the OS's transport, not something the app asked to be launched with.
		if (remainingArguments is not null)
		{
			Application.SetArguments(remainingArguments);
		}

		if (found)
		{
			AppInstance.GetCurrent().SetOrRaiseActivation(
				AppActivationArguments.CreateProtocol(new ProtocolActivatedEventArgs(uri!, ApplicationExecutionState.NotRunning)));
		}
	}
}
