using System.Runtime.Serialization;

namespace Uno.Storage.Pickers.Internal
{
	[DataContract]
	internal class NativeFilePickerAcceptTypeItem
	{
		[DataMember(Name = "mimeType")]
		public string MimeType { get; set; }

		[DataMember(Name = "extensions")]
		public string[] Extensions { get; set; }
	}
}
