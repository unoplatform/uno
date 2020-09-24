using System;

namespace Windows.ApplicationModel.Contacts
{
	[Flags]
	public enum ContactQuerySearchFields : uint
	{
		None = 0,
		Name = 1,
		Email = 2,
		Phone = 4,
		All = uint.MaxValue
	}
}
