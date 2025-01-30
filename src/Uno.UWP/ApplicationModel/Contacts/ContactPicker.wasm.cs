#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Uno.ApplicationModel.Contacts.Internal;
using Uno.Foundation;
using Uno.Helpers.Serialization;

using NativeMethods = __Windows.ApplicationModel.Contacts.ContactPicker.NativeMethods;

namespace Windows.ApplicationModel.Contacts
{
	public partial class ContactPicker
	{
		private static Task<bool> IsSupportedTaskAsync(CancellationToken token)
		{
			return Task.FromResult(NativeMethods.IsSupported());
		}

		private async Task<Contact[]> PickContactsAsync(bool multiple, CancellationToken token)
		{
			var pickResultJson = await NativeMethods.PickContactsAsync(multiple);

			if (string.IsNullOrEmpty(pickResultJson) || token.IsCancellationRequested)
			{
				return Array.Empty<Contact>();
			}

			var contacts = JsonHelper.Deserialize<WasmContact[]>(pickResultJson, PickerSerializationContext.Default);
			return contacts.Where(c => c != null).Select(c => ContactFromContactInfo(c)).ToArray();
		}

		private static Contact ContactFromContactInfo(WasmContact contactInfo)
		{
			var contact = new Contact();
			contact.DisplayNameOverride = contactInfo.Name?.FirstOrDefault(n => n?.Length > 0) ?? "";
			foreach (var phoneNumber in contactInfo.Tel?.Where(t => t?.Length > 0) ?? Array.Empty<string>())
			{
				var contactPhone = new ContactPhone()
				{
					Number = phoneNumber,
					Kind = ContactPhoneKind.Other
				};
				contact.Phones.Add(contactPhone);
			}

			foreach (var email in contactInfo.Email?.Where(t => t?.Length > 0) ?? Array.Empty<string>())
			{
				var contactEmail = new ContactEmail()
				{
					Address = email,
					Kind = ContactEmailKind.Other
				};
				contact.Emails.Add(contactEmail);
			}

			foreach (var address in contactInfo.Address?.Where(a => a != null) ?? Array.Empty<WasmContactAddress>())
			{
				var contactAddress = new ContactAddress()
				{
					StreetAddress = address.DependentLocality ?? "",
					Country = address.Country ?? "",
					Locality = address.City ?? "",
					Region = address.Region ?? "",
					PostalCode = address.PostalCode ?? "",
					Kind = ContactAddressKind.Other
				};
				contact.Addresses.Add(contactAddress);
			}

			return contact;
		}


		[JsonSerializable(typeof(WasmContact[]))]
		[JsonSerializable(typeof(WasmContact))]
		internal partial class PickerSerializationContext : JsonSerializerContext
		{
		}
	}
}
