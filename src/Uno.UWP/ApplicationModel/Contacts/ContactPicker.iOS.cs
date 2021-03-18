#nullable enable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Contacts;
using ContactsUI;
using UIKit;
using Uno.Helpers.Theming;
using Windows.ApplicationModel.Core;

namespace Windows.ApplicationModel.Contacts
{
	public partial class ContactPicker
	{
		private static Task<bool> IsSupportedTaskAsync(CancellationToken token) => Task.FromResult(true);

		private async Task<Contact[]> PickContactsAsync(bool multiple, CancellationToken token)
		{
			var window = UIApplication.SharedApplication.KeyWindow;
			var controller = window.RootViewController;
			if (controller == null)
			{
				throw new InvalidOperationException(
					$"The root view controller is not yet set, " +
					$"API was called too early in the application lifecycle.");
			}

			var completionSource = new TaskCompletionSource<CNContact[]>();

			using var picker = new CNContactPickerViewController
			{

				Delegate = multiple ?
					(ICNContactPickerDelegate)new MultipleContactPickerDelegate(completionSource) :
					(ICNContactPickerDelegate)new SingleContactPickerDelegate(completionSource),
			};

			picker.OverrideUserInterfaceStyle = CoreApplication.RequestedTheme == SystemTheme.Light ?
				UIUserInterfaceStyle.Light : UIUserInterfaceStyle.Dark;

			await controller.PresentViewControllerAsync(picker, true);

			var cnContacts = await completionSource.Task;
			if (token.IsCancellationRequested)
			{
				return Array.Empty<Contact>();
			}

			return cnContacts
				.Where(contact => contact != null)
				.Select(contact => CNContactToContact(contact))
				.ToArray();
		}

		private static Contact CNContactToContact(CNContact cnContact)
		{
			var contact = new Contact();

			contact.HonorificNamePrefix = cnContact.NamePrefix ?? string.Empty;
			contact.FirstName = cnContact.GivenName ?? string.Empty;
			contact.MiddleName = cnContact.MiddleName ?? string.Empty;
			contact.LastName = cnContact.FamilyName ?? string.Empty;
			contact.HonorificNameSuffix = cnContact.NameSuffix ?? string.Empty;

			if (string.IsNullOrWhiteSpace(contact.DisplayName) && !string.IsNullOrWhiteSpace(cnContact.OrganizationName))
			{
				contact.DisplayNameOverride = cnContact.OrganizationName;
			}

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

		private class SingleContactPickerDelegate : CNContactPickerDelegate
		{
			private readonly TaskCompletionSource<CNContact[]> _completionSource = null!;

			public SingleContactPickerDelegate(TaskCompletionSource<CNContact[]> completionSource)
			{
				_completionSource = completionSource;
			}

			public SingleContactPickerDelegate(IntPtr handle)
				: base(handle)
			{
			}

			public override void ContactPickerDidCancel(CNContactPickerViewController picker)
			{
				picker.DismissModalViewController(true);
				_completionSource.SetResult(Array.Empty<CNContact>());
			}

			public override void DidSelectContact(CNContactPickerViewController picker, CNContact contact)
			{
				picker.DismissModalViewController(true);
				_completionSource.SetResult(new[] { contact });
			}
		}

		private class MultipleContactPickerDelegate : CNContactPickerDelegate
		{
			private readonly TaskCompletionSource<CNContact[]> _completionSource = null!;

			public MultipleContactPickerDelegate(TaskCompletionSource<CNContact[]> completionSource)
			{
				_completionSource = completionSource;
			}

			public MultipleContactPickerDelegate(IntPtr handle)
				: base(handle)
			{
			}

			public override void ContactPickerDidCancel(CNContactPickerViewController picker)
			{
				picker.DismissModalViewController(true);
				_completionSource.SetResult(Array.Empty<CNContact>());
			}

			public override void DidSelectContacts(CNContactPickerViewController picker, CNContact[] contacts)
			{
				picker.DismissModalViewController(true);
				_completionSource.SetResult(contacts);
			}
		}
	}
}
