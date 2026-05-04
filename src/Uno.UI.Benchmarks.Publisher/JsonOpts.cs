using System.Text.Json;
using System.Text.Json.Serialization;

namespace Uno.UI.Benchmarks.Publisher;

internal static class JsonOpts
{
	public static readonly JsonSerializerOptions Default = new()
	{
		WriteIndented = false,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
	};

	public static readonly JsonSerializerOptions Indented = new()
	{
		WriteIndented = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
	};
}
