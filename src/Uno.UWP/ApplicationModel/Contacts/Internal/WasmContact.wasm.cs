#nullable enable

using System.Runtime.Serialization;

namespace Uno.ApplicationModel.Contacts.Internal
{
	[DataContract]
	internal class WasmContact
	{
		[DataMember(Name = "id")]
		public string? Id { get; set; }

		[DataMember(Name = "email")]
		public string[]? Email { get; set; }

		[DataMember(Name = "name")]
		public string[]? Name { get; set; }

		[DataMember(Name = "tel")]
		public string[]? Tel { get; set; }

		[DataMember(Name = "address")]
		public WasmContactAddress[]? Address { get; set; }
	}
}
