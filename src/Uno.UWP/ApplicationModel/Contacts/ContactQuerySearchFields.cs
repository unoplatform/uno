using System;

namespace Windows.ApplicationModel.Contacts
{
	[Flags]
	public enum ContactQuerySearchFields
	{
		None = 0,   // no search - all entries
		Name = 1,
		Email = 2,
		Phone = 4,
		All = -1 // 4294967295 == 0b_1111_1111_1111_1111_1111_1111_1111_1111 == ?FFFFFFFF?
	}
}
