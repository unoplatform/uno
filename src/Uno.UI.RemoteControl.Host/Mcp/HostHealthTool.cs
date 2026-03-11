using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Microsoft.Extensions.DependencyInjection;
using Uno.UI.RemoteControl.Helpers;
using Uno.UI.RemoteControl.Host.Extensibility;
using Uno.UI.RemoteControl.Host.IdeChannel;
using Uno.UI.RemoteControl.Services;

namespace Uno.UI.RemoteControl.Host.Mcp;

[McpServerToolType]
internal sealed class HostHealthTool
{
	internal const string ToolName = "uno_health";
	internal const string ResourceUri = "uno://health";

	[McpServerTool(Name = ToolName)]
	[Description("Returns the health status of the Uno DevServer Host, including add-in loading status, host version, and IDE channel connection state. Always available even before add-ins finish loading.")]
	public static string GetHealth(AddInsStatus addInsStatus, IIdeChannel ideChannel)
	{
		var report = BuildReport(addInsStatus, ideChannel);
		return JsonSerializer.Serialize(report, JsonOptions);
	}

	internal static HostHealthReport BuildReport(AddInsStatus addInsStatus, IIdeChannel ideChannel)
	{
		var addInEntries = addInsStatus.Assemblies.Select(a => new HostAddInEntry
		{
			Name = a.Assembly?.GetName().Name ?? Path.GetFileNameWithoutExtension(a.DllFile),
			Version = a.Assembly is not null ? VersionHelper.GetVersion(a.Assembly) : null,
			AssemblyPath = a.DllFile,
			Loaded = a.Assembly is not null,
			Error = a.Error?.Message,
		}).ToList();

		var hasErrors = addInsStatus.Discovery.Error is not null
			|| addInEntries.Any(a => !a.Loaded);

		var ideConnected = ideChannel is IdeChannelServer server && server.IsConnected;

		return new HostHealthReport
		{
			Status = hasErrors ? HostHealthStatus.Degraded : HostHealthStatus.Healthy,
			HostVersion = VersionHelper.GetVersion(typeof(HostHealthTool).Assembly),
			AddIns = addInEntries,
			IdeChannelConnected = ideConnected,
		};
	}

	internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
	{
		PropertyNameCaseInsensitive = true,
	};

	/// <summary>
	/// Registers the Host-level MCP health tool and resource on the given service collection.
	/// </summary>
	internal static void Configure(IServiceCollection services)
	{
		services.AddMcpServer()
			.WithTools<HostHealthTool>()
			.WithListResourcesHandler((ctx, ct) =>
			{
				var resource = new Resource
				{
					Uri = ResourceUri,
					Name = "Uno DevServer Health",
					Description = "Health status of the Uno DevServer Host, including add-in loading status and IDE channel state.",
					MimeType = "application/json",
				};

				return ValueTask.FromResult(new ListResourcesResult { Resources = [resource] });
			})
			.WithReadResourceHandler((ctx, ct) =>
			{
				var uri = ctx.Params?.Uri;
				if (!string.Equals(uri, ResourceUri, StringComparison.OrdinalIgnoreCase))
				{
					throw new McpException($"Unknown resource URI: {uri}");
				}

				var sp = ctx.Server!.Services!;
				var addInsStatus = sp.GetRequiredService<AddInsStatus>();
				var ideChannel = sp.GetRequiredService<IIdeChannel>();
				var report = BuildReport(addInsStatus, ideChannel);
				var json = JsonSerializer.Serialize(report, JsonOptions);

				var contents = new TextResourceContents
				{
					Uri = ResourceUri,
					Text = json,
					MimeType = "application/json",
				};

				return ValueTask.FromResult(new ReadResourceResult { Contents = [contents] });
			});
	}
}
