#nullable enable

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

		private static async Task<bool> IsSupportedTaskAsync()
		{
			return _contactPickerExtension != null && await _contactPickerExtension.IsSupportedAsync();
		}

		private async Task<Contact?> PickContactTaskAsync()
		{
			if (_contactPickerExtension != null)
			{
				return await _contactPickerExtension.PickContactAsync();
			}
			return null;
		}
	}

	internal interface IContactPickerExtension
	{
		Task<bool> IsSupportedAsync();

		Task<Contact?> PickContactAsync();
	}
}
