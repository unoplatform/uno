#nullable enable

using System;
using Windows.Foundation;

namespace Windows.ApplicationModel.Contacts
{
	public partial class ContactPicker
	{
		public ContactPicker()
		{
		}

		public static IAsyncOperation<bool> IsSupportedAsync() =>
			IsSupportedTaskAsync().AsAsyncOperation();

		public IAsyncOperation<Contact?> PickContactAsync() =>
			PickContactTaskAsync().AsAsyncOperation()
	}
}
