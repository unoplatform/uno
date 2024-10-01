#nullable enable

using System.Text.Json.Serialization;
using Uno.Storage.Internal;

namespace Windows.Storage.Pickers
{
	[JsonSerializable(typeof(NativeStorageItemInfo[]))]
	[JsonSerializable(typeof(NativeStorageItemInfo))]
	internal partial class StorageSerializationContext : JsonSerializerContext
	{
	}
}
