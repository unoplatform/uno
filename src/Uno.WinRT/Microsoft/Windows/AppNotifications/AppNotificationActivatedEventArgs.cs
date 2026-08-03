#nullable enable

using System.Collections.Generic;
using Microsoft.Windows.AppNotifications.Internal;
using Windows.Foundation.Metadata;

namespace Microsoft.Windows.AppNotifications;

[ContractVersion(typeof(AppNotificationsContract), 1 * 0x10000u)]
public sealed class AppNotificationActivatedEventArgs
{
	internal AppNotificationActivatedEventArgs(string argument, IDictionary<string, string>? userInput = null)
	{
		Argument = argument ?? string.Empty;
		Arguments = AppNotificationArgumentCodec.Decode(Argument);
		UserInput = userInput is null
			? new Dictionary<string, string>()
			: new Dictionary<string, string>(userInput);
	}

	public string Argument { get; }

	[ContractVersion(typeof(AppNotificationsContract), 3 * 0x10000u)]
	public IDictionary<string, string> Arguments { get; }

	public IDictionary<string, string> UserInput { get; }
}
