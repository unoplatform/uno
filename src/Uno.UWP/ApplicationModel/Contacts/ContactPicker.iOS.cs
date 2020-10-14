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

			contact.HonorificNamePrefix = cnContact.NamePrefix;
			contact.FirstName = cnContact.GivenName;
			contact.MiddleName = cnContact.MiddleName;
			contact.LastName = cnContact.FamilyName;
			contact.HonorificNameSuffix = cnContact.NameSuffix;			

			foreach (var item in cnContact.PhoneNumbers)
			{
				if (item != null)
				{
					contact.Phones.Add(new ContactPhone()
					{
						Number = item.Value.StringValue,
						Description = item.Label,
						Kind = ContactTypeToContactPhoneKind(cnContact.ContactType)
					});
				}
			}

			foreach (var email in cnContact.EmailAddresses)
			{
				if (email != null)
				{
					contact.Emails.Add(new ContactEmail()
					{
						Address = email.Value.ToString(),
						Description = email.Label,
						Kind = ContactTypeToContactEmailKind(cnContact.ContactType)
					});
				}
			}

			foreach (var address in cnContact.PostalAddresses)
			{
				if (address != null)
				{
					contact.Addresses.Add(new ContactAddress()
					{
						Country = address.Value.Country,
						PostalCode = address.Value.PostalCode,
						StreetAddress = address.Value.Street,
					});
				}
			}

			return contact;
		}

		private static ContactPhoneKind ContactTypeToContactPhoneKind(CNContactType type) =>
			type switch
			{
				CNContactType.Person => ContactPhoneKind.Mobile,
				CNContactType.Organization => ContactPhoneKind.Company,
				_ => ContactPhoneKind.Other,
			};

		private static ContactEmailKind ContactTypeToContactEmailKind(CNContactType type) =>
			type switch
			{
				CNContactType.Person => ContactEmailKind.Personal,
				CNContactType.Organization => ContactEmailKind.Work,
				_ => ContactEmailKind.Other
			};

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
