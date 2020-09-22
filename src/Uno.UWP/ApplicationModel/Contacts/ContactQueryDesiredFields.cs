using System;

namespace Windows.ApplicationModel.Contacts
{
	[Flags]
	public enum ContactQueryDesiredFields
	{
		None = 0,
		PhoneNumber = 1,
		EmailAddress = 2,
		PostalAddress = 4
	}
}
