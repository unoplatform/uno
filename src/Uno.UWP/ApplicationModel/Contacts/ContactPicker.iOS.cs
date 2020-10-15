#nullable enable

using System;
using System.Threading.Tasks;
using Contacts;
using ContactsUI;
using UIKit;

namespace Windows.ApplicationModel.Contacts
{
	public partial class ContactPicker
	{
		private static Task<bool> IsSupportedTaskAsync() => Task.FromResult(true);

		private async Task<Contact?> PickContactTaskAsync()
		{
			var window = UIApplication.SharedApplication.KeyWindow;
			var controller = window.RootViewController;
			if (controller == null)
			{
				throw new InvalidOperationException(
					$"The root view controller is not yet set, " +
					$"API was called too early in the application lifecycle.");
			}

			var completionSource = new TaskCompletionSource<CNContact?>();

			using var picker = new CNContactPickerViewController
			{
				Delegate = new ContactPickerDelegate(completionSource)
			};

			await controller.PresentViewControllerAsync(picker, true);

			var cnContact = await completionSource.Task;
			if (cnContact != null)
			{
				return CNContactToContact(cnContact);
			}
			return null;
		}

		private static Contact CNContactToContact(CNContact cnContact)
		{
			var contact = new Contact();

			contact.HonorificNamePrefix = cnContact.NamePrefix ?? string.Empty;
			contact.FirstName = cnContact.GivenName ?? string.Empty;
			contact.MiddleName = cnContact.MiddleName ?? string.Empty;
			contact.LastName = cnContact.FamilyName ?? string.Empty;
			contact.HonorificNameSuffix = cnContact.NameSuffix ?? string.Empty;

			foreach (var phoneNumber in cnContact.PhoneNumbers)
			{
				if (phoneNumber != null)
				{
					contact.Phones.Add(new ContactPhone()
					{
						Number = phoneNumber.Value.StringValue ?? string.Empty,
						Kind = PhoneLabelToContactPhoneKind(phoneNumber.Label)
					});
				}
			}

			foreach (var email in cnContact.EmailAddresses)
			{
				if (email != null)
				{
					contact.Emails.Add(new ContactEmail()
					{
						Address = email.Value?.ToString() ?? string.Empty,
						Kind = EmailLabelToContactEmailKind(email.Label)
					});
				}
			}

			foreach (var address in cnContact.PostalAddresses)
			{
				if (address != null)
				{
					contact.Addresses.Add(new ContactAddress()
					{
						Country = address.Value.Country ?? string.Empty,
						PostalCode = address.Value.PostalCode ?? string.Empty,
						StreetAddress = address.Value.Street ?? string.Empty,
						Locality = address.Value.City ?? string.Empty,
						Region = address.Value.State ?? string.Empty,
						Kind = AddressLabelToContactAddressKind(address.Label)
					});
				}
			}

			return contact;
		}

		private static ContactPhoneKind PhoneLabelToContactPhoneKind(string? label)
		{
			if (CNLabelPhoneNumberKey.Mobile.ToString().Equals(label, StringComparison.InvariantCultureIgnoreCase) ||
				CNLabelPhoneNumberKey.Main.ToString().Equals(label, StringComparison.InvariantCultureIgnoreCase) ||
				CNLabelPhoneNumberKey.iPhone.ToString().Equals(label, StringComparison.InvariantCultureIgnoreCase))
			{
				return ContactPhoneKind.Mobile;
			}
			else if (CNLabelKey.Home.ToString().Equals(label, StringComparison.InvariantCultureIgnoreCase))
			{
				return ContactPhoneKind.Home;
			}
			else if (CNLabelPhoneNumberKey.HomeFax.ToString().Equals(label, StringComparison.InvariantCultureIgnoreCase))
			{
				return ContactPhoneKind.HomeFax;
			}
			else if (CNLabelKey.Work.ToString().Equals(label, StringComparison.InvariantCultureIgnoreCase))
			{
				return ContactPhoneKind.Work;
			}
			else if (CNLabelPhoneNumberKey.WorkFax.ToString().Equals(label, StringComparison.InvariantCultureIgnoreCase))
			{
				return ContactPhoneKind.BusinessFax;
			}
			else if (CNLabelPhoneNumberKey.Pager.ToString().Equals(label, StringComparison.InvariantCultureIgnoreCase))
			{
				return ContactPhoneKind.Pager;
			}
			else
			{
				return ContactPhoneKind.Other;
			}
		}

		private static ContactEmailKind EmailLabelToContactEmailKind(string? label)
		{
			if (CNLabelKey.Home.ToString().Equals(label, StringComparison.InvariantCultureIgnoreCase))
			{
				return ContactEmailKind.Personal;
			}
			else if (CNLabelKey.Work.ToString().Equals(label, StringComparison.InvariantCultureIgnoreCase))
			{
				return ContactEmailKind.Work;
			}
			else
			{
				return ContactEmailKind.Other;
			}
		}

		private static ContactAddressKind AddressLabelToContactAddressKind(string? label)
		{
			if (CNLabelKey.Home.ToString().Equals(label, StringComparison.InvariantCultureIgnoreCase))
			{
				return ContactAddressKind.Home;
			}
			else if (CNLabelKey.Work.ToString().Equals(label, StringComparison.InvariantCultureIgnoreCase))
			{
				return ContactAddressKind.Work;
			}
			else
			{
				return ContactAddressKind.Other;
			}
		}

		private class ContactPickerDelegate : CNContactPickerDelegate
		{
			private readonly TaskCompletionSource<CNContact?> _completionSource = null!;

			public ContactPickerDelegate(TaskCompletionSource<CNContact?> completionSource)
			{
				_completionSource = completionSource;
			}

			public ContactPickerDelegate(IntPtr handle)
				: base(handle)
			{
			}

			public Action<CNContact?> DidSelectContactHandler { get; } = null!;

			public override void ContactPickerDidCancel(CNContactPickerViewController picker)
			{
				picker.DismissModalViewController(true);
				_completionSource.SetResult(null);
			}

			public override void DidSelectContact(CNContactPickerViewController picker, CNContact contact)
			{
				picker.DismissModalViewController(true);
				_completionSource.SetResult(contact);
			}

			public override void DidSelectContactProperty(CNContactPickerViewController picker, CNContactProperty contactProperty) =>
				picker.DismissModalViewController(true);
		}
	}
}
