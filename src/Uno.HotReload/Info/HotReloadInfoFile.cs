using System.IO;
using System.Threading.Tasks;
using Uno.HotReload.IO;
using Uno.HotReload.Tracking;
using Uno.UI.Tasks.HotReloadInfo;

namespace Uno.HotReload.Info;

/// <summary>
/// Writes hot-reload info to a generated file so the client application
/// can determine which update request has been applied.
/// </summary>
public class HotReloadInfoFile(string? path, IReporter? reporter = null)
{
	public async ValueTask SetAsync(HotReloadOperation operation, IUpdateFileRequest? request = null)
	{
		if (path is not { Length: > 0 })
		{
			return;
		}

		var effectiveUpdate = FileSystemHelper.WaitForFileUpdated(path, reporter);
		await File.WriteAllTextAsync(path, HotReloadInfoHelper.GenerateInfo(operation.Id, request?.RequestId));
		await effectiveUpdate;
	}
}
