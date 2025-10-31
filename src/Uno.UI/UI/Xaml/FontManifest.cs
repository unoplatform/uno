using System.Text.Json;
using System.Text.Json.Serialization;

namespace Uno.UI.Xaml.Media;

[JsonSourceGenerationOptions(
	AllowTrailingCommas = true,
	ReadCommentHandling = JsonCommentHandling.Skip,
	GenerationMode = JsonSourceGenerationMode.Serialization,
	PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(FontManifest))]
internal sealed partial class FontManifestSerializerContext : JsonSerializerContext
{

}

internal sealed class FontManifest
{
	public FontInfo[] Fonts { get; set; }
}
