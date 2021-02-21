#nullable enable

using System;
using System.Runtime.Serialization;

namespace Uno.Storage.Internal
{
	[DataContract]
	internal class NativeStorageItemInfo
	{
		[DataMember(Name = "id")]
		public Guid Id { get; set; }

		[DataMember(Name = "name")]
		public string Name { get; set; } = null!;

		[DataMember(Name = "isFile")]
		public bool IsFile { get; set; }
	}
}
