using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Uno.Sdk;

internal record PackageReference(string PackageId, string Version, bool Override)
{
	public ITaskItem ToTaskItem()
	{
		var taskItem = new TaskItem
		{
			ItemSpec = PackageId,
		};
		var versionMetadta = Override ? "VersionOverride" : "Version";
		taskItem.SetMetadata(versionMetadta, Version);
		taskItem.SetMetadata("IsImplicitlyDefined", bool.TrueString);
		return taskItem;
	}
}
