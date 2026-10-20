#nullable enable

using System.Text.Json.Serialization;
using Uno.Storage.Internal;
using Uno.Storage.Pickers.Internal;

namespace Windows.Storage.Pickers
{
	[JsonSerializable(typeof(NativeStorageItemInfo[]))]
	[JsonSerializable(typeof(NativeStorageItemInfo))]
	[JsonSerializable(typeof(NativeFilePickerAcceptType))]
	[JsonSerializable(typeof(NativeFilePickerAcceptType[]))]
	internal partial class StorageSerializationContext : JsonSerializerContext
	{
	}
}
