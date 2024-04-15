using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

#nullable enable
namespace Uno.Sdk.Models;

internal record PackageReference(string PackageId, string Version, string? ExcludeAssets = null)
{
	public ITaskItem ToTaskItem()
	{
		var taskItem = new TaskItem
		{
			ItemSpec = PackageId,
		};
		taskItem.SetMetadata(nameof(Version), Version);
		taskItem.SetMetadata("IsImplicitlyDefined", bool.TrueString);
		if (!string.IsNullOrEmpty(ExcludeAssets))
		{
			taskItem.SetMetadata(nameof(ExcludeAssets), ExcludeAssets);
		}
		return taskItem;
	}
}
