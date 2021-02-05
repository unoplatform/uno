#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation.Extensibility;

namespace Windows.ApplicationModel.Contacts
{
	public partial class ContactPicker
	{
		private static IContactPickerExtension? _contactPickerExtension;

		static ContactPicker()
		{
			if (_contactPickerExtension == null)
			{
				ApiExtensibility.CreateInstance(typeof(ContactPicker), out _contactPickerExtension);
			}
		}

		private static async Task<bool> IsSupportedTaskAsync(CancellationToken token)
		{
			return _contactPickerExtension != null && await _contactPickerExtension.IsSupportedAsync(token);
		}

		private async Task<Contact[]> PickContactsAsync(bool multiple, CancellationToken token)
		{
			if (_contactPickerExtension != null)
			{
				return await _contactPickerExtension.PickContactsAsync(multiple, token);
			}
			return Array.Empty<Contact>();
		}
	}

	internal interface IContactPickerExtension
	{
		Task<bool> IsSupportedAsync(CancellationToken token);

		Task<Contact[]> PickContactsAsync(bool multiple, CancellationToken token);
	}
}
