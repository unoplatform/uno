using System.Runtime.Serialization;

namespace Uno.Storage.Pickers.Internal
{
	internal class NativeFilePickerAcceptTypeItem
	{
		[global::System.Text.Json.Serialization.JsonPropertyName("mimeType")]
		public string? MimeType { get; set; }

		[global::System.Text.Json.Serialization.JsonPropertyName("extensions")]
		public string[]? Extensions { get; set; }
	}
}
