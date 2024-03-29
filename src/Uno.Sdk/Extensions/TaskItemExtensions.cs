using System.Linq;

namespace Microsoft.Build.Framework;

internal static class TaskItemExtensions
{
	public static bool HasMetadata(this ITaskItem item, string name) =>
		item.MetadataNames.OfType<object>().Select(x => x.ToString()).Any(x => x.Equals(name));
}
