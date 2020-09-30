using System.Runtime.Serialization;

namespace Uno.Storage.Pickers.Internal
{
	[DataContract]
	internal class NativeFilePickerAcceptType
	{
		[DataMember(Name = "description")]
		public string Description { get; set; }

		[DataMember(Name = "accept")]
		public NativeFilePickerAcceptTypeItem[] Accept { get; set; }
	}
}
