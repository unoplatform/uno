#nullable enable
#pragma warning disable CS8305

using Uno.Foundation.Extensibility;
using Uno.UI.Shell.Tasks;

namespace Windows.UI.Shell.Tasks;

internal static partial class AppTaskInfoPlatform
{
	internal static partial IAppTaskInfoExtension? CreateExtension() =>
		ApiExtensibility.CreateInstance<IAppTaskInfoExtension>(typeof(AppTaskInfo), out var extension)
			? extension
			: null;
}
