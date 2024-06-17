using System.Text.Json.Serialization;

namespace Uno.UI.Xaml.Media;

[JsonSerializable(typeof(FontManifest))]
internal sealed partial class FontManifestSerializerContext : JsonSerializerContext
{

}

internal sealed class FontManifest
{
	public FontInfo[] Fonts { get; set; }
}
