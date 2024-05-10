using System.Collections;
using System.Collections.Generic;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

#nullable enable
namespace Uno.Sdk.Models;

internal record PackageReference(string PackageId, string Version, IDictionary<string, string> MetaData)
{
	public ITaskItem ToTaskItem()
	{
		var taskItem = new TaskItem
		{
			ItemSpec = PackageId,
		};

		foreach (var data in MetaData)
		{
			if (data.Key == "ProjectSystem")
				continue;

			taskItem.SetMetadata(data.Key, data.Value);
		}

		taskItem.SetMetadata(nameof(Version), Version);
		taskItem.SetMetadata("IsImplicitlyDefined", bool.TrueString);
		taskItem.SetMetadata("Sdk", "Uno");
		return taskItem;
	}
}
