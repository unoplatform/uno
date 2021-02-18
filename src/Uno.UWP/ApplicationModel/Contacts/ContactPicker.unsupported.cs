#if !__ANDROID__ && !__IOS__ && !__WASM__ && !__SKIA__
#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Windows.ApplicationModel.Contacts
{
	public partial class ContactPicker
	{
		private static Task<bool> IsSupportedTaskAsync(CancellationToken token) =>
			Task.FromResult(false);

		private Task<Contact[]> PickContactsAsync(bool multiple, CancellationToken token) =>
			Task.FromResult<Contact[]>(Array.Empty<Contact>());
	}
}
#endif
