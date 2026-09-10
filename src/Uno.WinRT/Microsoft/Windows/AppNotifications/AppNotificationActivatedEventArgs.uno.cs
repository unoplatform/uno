#nullable enable

using System.Collections.Generic;

namespace Microsoft.Windows.AppNotifications;

partial class AppNotificationActivatedEventArgs
{
	// Platform activation transports may reuse their mutable input dictionaries.
	private static IDictionary<string, string> SnapshotUserInput(IDictionary<string, string>? userInput)
		=> userInput is null ? new Dictionary<string, string>() : new Dictionary<string, string>(userInput);
}
