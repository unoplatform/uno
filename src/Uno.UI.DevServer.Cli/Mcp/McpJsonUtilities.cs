using System.Text.Json;

namespace Uno.UI.DevServer.Cli.Mcp;

internal static class McpJsonUtilities
{
	public static JsonSerializerOptions DefaultOptions { get; } = new(JsonSerializerDefaults.Web)
	{
		PropertyNameCaseInsensitive = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
	};
}
