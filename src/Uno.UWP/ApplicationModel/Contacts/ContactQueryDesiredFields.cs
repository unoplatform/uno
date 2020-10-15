using System;

namespace Windows.ApplicationModel.Contacts
{
	/// <summary>
	/// Defines which fields must exist on a contact in order to match a search operation.
	/// </summary>
	[Flags]
	public enum ContactQueryDesiredFields
	{
		/// <summary>
		/// No required fields.
		/// </summary>
		None = 0,
		/// <summary>
		/// The contact must have a phone number.
		/// </summary>
		PhoneNumber = 1,
		/// <summary>
		/// The contact must have an email address.
		/// </summary>
		EmailAddress = 2,
		/// <summary>
		/// The contact must have a postal address.
		/// </summary>
		PostalAddress = 4
	}
}
