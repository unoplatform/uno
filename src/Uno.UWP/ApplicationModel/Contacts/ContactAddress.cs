#nullable enable

namespace Windows.ApplicationModel.Contacts
{
	/// <summary>
	/// Represents the address of a contact.
	/// </summary>
	public partial class ContactAddress
	{
		/// <summary>
		/// Gets and sets the kind of contact address.
		/// </summary>
		public ContactAddressKind Kind { get; set; } = ContactAddressKind.Home;

		/// <summary>
		/// Gets and sets the street address of a contact address.
		/// </summary>
		public string StreetAddress { get; set; } = string.Empty;

		/// <summary>
		/// Gets and sets the region of a contact address.
		/// </summary>
		public string Region { get; set; } = string.Empty;

		/// <summary>
		/// Gets and sets the postal code of a contact address.
		/// </summary>
		public string PostalCode { get; set; } = string.Empty;

		/// <summary>
		/// Gets and sets the locality of a contact address.
		/// </summary>
		public string Locality { get; set; } = string.Empty;

		/// <summary>
		/// Gets and sets the country of a contact address.
		/// </summary>
		public string Country { get; set; } = string.Empty;

		/// <summary>
		/// Gets and sets the description of a contact address.
		/// </summary>
		public string Description { get; set; } = string.Empty;
	}
}
