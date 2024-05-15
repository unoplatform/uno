#nullable enable

using System.Runtime.Serialization;

namespace Uno.ApplicationModel.Contacts.Internal
{
	internal class WasmContact
	{
		[global::System.Text.Json.Serialization.JsonPropertyName("id")]
		public string? Id { get; set; }

		[global::System.Text.Json.Serialization.JsonPropertyName("email")]
		public string[]? Email { get; set; }

		[global::System.Text.Json.Serialization.JsonPropertyName("name")]
		public string[]? Name { get; set; }

		[global::System.Text.Json.Serialization.JsonPropertyName("tel")]
		public string[]? Tel { get; set; }

		[global::System.Text.Json.Serialization.JsonPropertyName("address")]
		public WasmContactAddress[]? Address { get; set; }
	}
}
