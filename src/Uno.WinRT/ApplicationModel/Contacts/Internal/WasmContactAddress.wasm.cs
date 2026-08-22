#nullable enable

using System.Runtime.Serialization;

namespace Uno.ApplicationModel.Contacts.Internal
{
	internal class WasmContactAddress
	{
		[global::System.Text.Json.Serialization.JsonPropertyName("city")]
		public string? City { get; set; }

		[global::System.Text.Json.Serialization.JsonPropertyName("country")]
		public string? Country { get; set; }

		[global::System.Text.Json.Serialization.JsonPropertyName("dependentLocality")]
		public string? DependentLocality { get; set; }

		[global::System.Text.Json.Serialization.JsonPropertyName("organization")]
		public string? Organization { get; set; }

		[global::System.Text.Json.Serialization.JsonPropertyName("phone")]
		public string? Phone { get; set; }

		[global::System.Text.Json.Serialization.JsonPropertyName("postalCode")]
		public string? PostalCode { get; set; }

		[global::System.Text.Json.Serialization.JsonPropertyName("recipient")]
		public string? Recipient { get; set; }

		[global::System.Text.Json.Serialization.JsonPropertyName("region")]
		public string? Region { get; set; }

		[global::System.Text.Json.Serialization.JsonPropertyName("sortingCode")]
		public string? SortingCode { get; set; }

		[global::System.Text.Json.Serialization.JsonPropertyName("addressLine")]
		public string?[]? AddressLine { get; set; }
	}
}
