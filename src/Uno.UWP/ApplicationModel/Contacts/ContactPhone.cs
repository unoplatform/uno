namespace Windows.ApplicationModel.Contacts
{
	/// <summary>
	/// Represents information about the phone for a contact.
	/// </summary>
	public partial class ContactPhone
	{
		/// <summary>
		/// Gets and sets the kind of phone for a contact.
		/// </summary>
		public ContactPhoneKind Kind { get; set; } = ContactPhoneKind.Home;

		/// <summary>
		/// Gets and sets the phone number of a phone for a contact.
		/// </summary>
		public string Number { get; set; } = string.Empty;

		/// <summary>
		/// Gets and sets the description of the phone for a contact.
		/// </summary>
		public string Description { get; set; } = string.Empty;
	}
}
