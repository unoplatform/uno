#if NET461 || __NETSTD_REFERENCE__
#nullable enable

namespace Windows.ApplicationModel.Contacts
{
	public partial class ContactPicker
	{
		private static Task<bool> IsSupportedTaskAsync => Task.FromResult(false);

		private Task<Contact?> PickContactTaskAsync() => Task.FromResult<Contact?>(null);
	}
}
#endif
