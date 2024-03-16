using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Uno.Sdk;

internal record PackageReference(string PackageId, string Version)
{
	public ITaskItem ToTaskItem()
	{
		var taskItem = new TaskItem
		{
			ItemSpec = PackageId,
		};
		taskItem.SetMetadata("Version", Version);
		taskItem.SetMetadata("IsImplicitlyDefined", bool.TrueString);
		return taskItem;
	}
}
