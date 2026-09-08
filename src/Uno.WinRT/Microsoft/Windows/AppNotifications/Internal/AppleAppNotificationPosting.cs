#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.Windows.AppNotifications.Internal;

internal static class AppleAppNotificationPosting
{
	public static bool TryPost(
		AppleAppNotificationCommand command,
		Func<AppleAppNotificationCommand, IReadOnlyCollection<string>> getReplacedRequestIdentifiers,
		Func<AppleAppNotificationCommand, bool> tryAdd,
		Action<IReadOnlyCollection<string>> removeReplacedRequestIdentifiers)
	{
		ArgumentNullException.ThrowIfNull(command);
		ArgumentNullException.ThrowIfNull(getReplacedRequestIdentifiers);
		ArgumentNullException.ThrowIfNull(tryAdd);
		ArgumentNullException.ThrowIfNull(removeReplacedRequestIdentifiers);

		var postingCommand = AppleAppNotificationTranslator.PrepareForPosting(command);
		var replacedRequestIdentifiers = getReplacedRequestIdentifiers(postingCommand);
		if (!tryAdd(postingCommand))
		{
			return false;
		}
		if (replacedRequestIdentifiers.Count > 0)
		{
			removeReplacedRequestIdentifiers(replacedRequestIdentifiers);
		}
		return true;
	}
}
