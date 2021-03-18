#nullable enable

namespace Windows.ApplicationModel.Contacts
{
	/// <summary>
	/// Represents an email address of a contact.
	/// </summary>
	public partial class ContactEmail
	{
		/// <summary>
		/// Gets and sets the kind of email address of a contact.
		/// </summary>
		public ContactEmailKind Kind { get; set; } = ContactEmailKind.Personal;

		/// <summary>
		/// Gets and sets the email address of a contact.
		/// </summary>
		public string Address { get; set; } = string.Empty;

		/// <summary>
		/// Gets and sets the description of an email address of a contact.
		/// </summary>
		public string Description { get; set; } = string.Empty;
	}
}
