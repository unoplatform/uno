#nullable enable

using System;
using System.Runtime.Serialization;

namespace Uno.Storage.Internal
{
	internal class NativeStorageItemInfo
	{
		[global::System.Text.Json.Serialization.JsonPropertyName("id")]
		public Guid Id { get; set; }

		[global::System.Text.Json.Serialization.JsonPropertyName("name")]
		public string Name { get; set; } = null!;

		[global::System.Text.Json.Serialization.JsonPropertyName("isFile")]
		public bool IsFile { get; set; }
	}
}
