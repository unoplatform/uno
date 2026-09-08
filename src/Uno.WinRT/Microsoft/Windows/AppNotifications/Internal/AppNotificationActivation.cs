#nullable enable

using System.Collections.Generic;

namespace Microsoft.Windows.AppNotifications.Internal;

internal sealed record AppNotificationActivation(string Argument, IDictionary<string, string> UserInput);
