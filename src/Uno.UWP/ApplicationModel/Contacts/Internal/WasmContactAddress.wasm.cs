#nullable enable

using System.Runtime.Serialization;

namespace Uno.ApplicationModel.Contacts.Internal
{
	[DataContract]
	internal class WasmContactAddress
	{
		[DataMember(Name = "city")]
		public string? City { get; set; }

		[DataMember(Name = "country")]
		public string? Country { get; set; }

		[DataMember(Name = "dependentLocality")]
		public string? DependentLocality { get; set; }

		[DataMember(Name = "organization")]
		public string? Organization { get; set; }

		[DataMember(Name = "phone")]
		public string? Phone { get; set; }

		[DataMember(Name = "postalCode")]
		public string? PostalCode { get; set; }

		[DataMember(Name = "recipient")]
		public string? Recipient { get; set; }

		[DataMember(Name = "region")]
		public string? Region { get; set; }

		[DataMember(Name = "sortingCode")]
		public string? SortingCode { get; set; }

		[DataMember(Name = "addressLine")]
		public string?[]? AddressLine { get; set; }
	}
}
