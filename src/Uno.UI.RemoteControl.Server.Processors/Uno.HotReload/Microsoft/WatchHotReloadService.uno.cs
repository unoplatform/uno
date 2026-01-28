using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Uno.HotReload.Microsoft;

namespace Uno.HotReload.Microsoft;

partial class WatchHotReloadService
{
	public static async ValueTask<WatchHotReloadService> CreateAsync(Workspace workspace, string[] metadataUpdateCapabilities, CancellationToken ct)
	{
		var currentSolution = workspace.CurrentSolution;
		var hotReloadService = new WatchHotReloadService(workspace.Services, metadataUpdateCapabilities);
		await hotReloadService.StartSessionAsync(currentSolution, ct);

		// Read the documents to memory
		await Task.WhenAll(currentSolution.Projects.SelectMany(p => p.Documents.Concat(p.AdditionalDocuments)).Select(d => d.GetTextAsync(ct)));

		// Warm up the compilation. This would help make the deltas for first edit appear much more quickly
		foreach (var project in currentSolution.Projects)
		{
			var c = await project.GetCompilationAsync(ct);
		}

		return hotReloadService;
	}
}
