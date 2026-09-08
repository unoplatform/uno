#nullable enable

using System.IO;

namespace Microsoft.Windows.AppNotifications.Internal;

internal static partial class AppleAppNotificationNativeContent
{
	internal static string? ResolveAttachmentPath(string source, string installedPath)
		=> AppleAppNotificationAssetPathResolver.TryResolve(source, installedPath, out var path) && File.Exists(path)
			? path
			: null;
}
