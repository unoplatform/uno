#nullable enable
#pragma warning disable CS8305

using Uno.UI.Shell.Tasks;

namespace Windows.UI.Shell.Tasks;

internal static partial class AppTaskInfoPlatform
{
	internal static partial IAppTaskInfoExtension? CreateExtension();
}
