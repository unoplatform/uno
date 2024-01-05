using System.Runtime.Serialization;

namespace Uno.Storage.Pickers.Internal
{
	internal class NativeFilePickerAcceptType
	{
		[global::System.Text.Json.Serialization.JsonPropertyName("description")]
		public string Description { get; set; } = null!;

		[global::System.Text.Json.Serialization.JsonPropertyName("accept")]
		public NativeFilePickerAcceptTypeItem[] Accept { get; set; } = null!;
	}
}
