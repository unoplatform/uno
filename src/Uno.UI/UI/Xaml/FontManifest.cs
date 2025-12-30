using System.Text.Json;
using System.Text.Json.Serialization;

namespace Uno.UI.Xaml.Media;

[JsonSourceGenerationOptions(
	AllowTrailingCommas = true,
	GenerationMode = JsonSourceGenerationMode.Metadata | JsonSourceGenerationMode.Serialization,
	PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
	ReadCommentHandling = JsonCommentHandling.Skip,
	UseStringEnumConverter = true)]
[JsonSerializable(typeof(FontManifest))]
internal sealed partial class FontManifestSerializerContext : JsonSerializerContext
{

}

internal sealed class FontManifest
{
	public FontInfo[] Fonts { get; set; }
}
