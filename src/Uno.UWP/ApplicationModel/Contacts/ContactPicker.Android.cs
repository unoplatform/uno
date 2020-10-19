#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Database;
using Android.Provider;
using Uno.Extensions;
using Uno.Helpers.Activities;
using Uno.UI;
using Windows.Extensions;

namespace Windows.ApplicationModel.Contacts
{
	public partial class ContactPicker
	{
		private static Task<bool> IsSupportedTaskAsync() => Task.FromResult(true);

		private async Task<Contact?> PickContactTaskAsync()
		{
			if (!await PermissionsHelper.CheckReadContactsPermission(default))
			{
				if (!await PermissionsHelper.TryGetReadContactsPermission(default))
				{
					throw new InvalidOperationException("android.permission.READ_CONTACTS permission is required");
				}
			}

			if (ContextHelper.Current == null)
			{
				throw new InvalidOperationException("Context is not initialized yet, API called too early in application lifecycle.");
			}

			using var intent = new Intent(Intent.ActionPick);
			intent.SetType(ContactsContract.CommonDataKinds.Phone.ContentType);
			var activity = await AwaitableResultActivity.StartAsync();
			var result = await activity.StartActivityForResultAsync(intent);

			if (result?.Intent?.Data is Android.Net.Uri contactUri)
			{
				using var contentResolver = Application.Context.ContentResolver;
				if (contentResolver != null)
				{
					var projection = new string[] { ContactsContract.ContactsColumns.LookupKey };
					using ICursor? cursorLookUpKey = contentResolver.Query(contactUri, projection, null, null, null);
					if (cursorLookUpKey?.MoveToFirst() == true)
					{
						var lookupKey = cursorLookUpKey.GetString(cursorLookUpKey.GetColumnIndex(projection[0]));
						if (lookupKey != null)
						{
							return LookupToContact(lookupKey, contentResolver);
						}
					}
				}
			}
			return null;
		}

		private static Contact LookupToContact(string lookupKey, ContentResolver contentResolver)
		{
			var contact = new Contact();


			ReadStructuredName(contact, lookupKey, contentResolver);

			//var displayName = cursor.GetString(cursor.GetColumnIndex(ContactsContract.Contacts.InterfaceConsts.DisplayName));
			//if (contact.DisplayName != displayName)
			//{
			//	contact.DisplayNameOverride = displayName ?? string.Empty;
			//}

			//contact.Notes = cursor.GetString(cursor.GetColumnIndex(ContactsContract.CommonDataKinds.Note.ContentItemType)) ?? string.Empty;





			//contact.Phones.AddRange(GetPhones(idQuery, contentResolver));

			//contact.Emails.AddRange(GetEmails(idQuery, contentResolver));

			//contact.Addresses.AddRange(GetAddresses(idQuery, contentResolver));			

			return contact;
		}

		private static void ReadStructuredName(Contact contact, string lookupKey, ContentResolver contentResolver)
		{
			var contactWhere = ContactsContract.ContactsColumns.LookupKey + " = ? AND " + ContactsContract.DataColumns.Mimetype + " = ?";
			var contactWhereParams = new[] { lookupKey, ContactsContract.CommonDataKinds.StructuredName.ContentItemType };
			if (ContactsContract.Data.ContentUri != null)
			{
				using var cursor = contentResolver.Query(
					ContactsContract.Data.ContentUri,
					null,
					contactWhere,
					contactWhereParams,
					null
				);
				if (cursor?.MoveToFirst() == true)
				{
					contact.HonorificNamePrefix = cursor.GetString(cursor.GetColumnIndex(ContactsContract.CommonDataKinds.StructuredName.Prefix)) ?? string.Empty;
					contact.FirstName = cursor.GetString(cursor.GetColumnIndex(ContactsContract.CommonDataKinds.StructuredName.GivenName)) ?? string.Empty;
					contact.MiddleName = cursor.GetString(cursor.GetColumnIndex(ContactsContract.CommonDataKinds.StructuredName.MiddleName)) ?? string.Empty;
					contact.LastName = cursor.GetString(cursor.GetColumnIndex(ContactsContract.CommonDataKinds.StructuredName.FamilyName)) ?? string.Empty;
					contact.HonorificNameSuffix = cursor.GetString(cursor.GetColumnIndex(ContactsContract.CommonDataKinds.StructuredName.Suffix)) ?? string.Empty;

					contact.YomiGivenName = cursor.GetString(cursor.GetColumnIndex(ContactsContract.CommonDataKinds.StructuredName.PhoneticGivenName)) ?? string.Empty;
					contact.YomiFamilyName = cursor.GetString(cursor.GetColumnIndex(ContactsContract.CommonDataKinds.StructuredName.PhoneticFamilyName)) ?? string.Empty;
				}
			}
		}

		private static IEnumerable<ContactPhone> GetPhones(string[] idQuery, ContentResolver contentResolver)
		{
			var contactPhones = new List<ContactPhone>();
			var uri = ContactsContract.CommonDataKinds.Phone.ContentUri?.BuildUpon()?.AppendQueryParameter(ContactsContract.RemoveDuplicateEntries, "1")?.Build();

			if (uri != null)
			{
				using ICursor? cursor = contentResolver.Query(
				   uri,
				   null,
				   ContactsContract.Contacts.InterfaceConsts.Id + "=?",
				   idQuery,
				   null);

				if (cursor != null)
				{
					if (cursor.MoveToFirst())
					{
						do
						{
							var number = cursor.GetString(cursor.GetColumnIndex(ContactsContract.CommonDataKinds.Phone.Number));

							var phoneType = cursor.GetString(cursor.GetColumnIndex(ContactsContract.CommonDataKinds.Phone.InterfaceConsts.Type));
							var kind = TypeToContactPhoneKind(phoneType);

							contactPhones.Add(new ContactPhone()
							{
								Kind = kind,
								Number = number ?? string.Empty
							});
						}
						while (cursor.MoveToNext());
					}
					cursor.Close();
				}
			}
			return contactPhones;
		}

		private static IEnumerable<ContactEmail> GetEmails(string[] idQuery, ContentResolver contentResolver)
		{
			var contactEmails = new List<ContactEmail>();
			var uri = ContactsContract.CommonDataKinds.Email.ContentUri?.BuildUpon()?.AppendQueryParameter(ContactsContract.RemoveDuplicateEntries, "1")?.Build();

			if (uri != null)
			{
				using ICursor? cursor = contentResolver.Query(
				   uri,
				   null,
				   ContactsContract.Contacts.InterfaceConsts.Id + "=?",
				   idQuery,
				   null);

				if (cursor != null)
				{
					if (cursor.MoveToFirst())
					{
						do
						{
							var address = cursor.GetString(cursor.GetColumnIndex(ContactsContract.CommonDataKinds.Email.Address));

							var emailType = cursor.GetString(cursor.GetColumnIndex(ContactsContract.CommonDataKinds.Email.InterfaceConsts.Type));
							var kind = TypeToContactEmailKind(emailType);

							contactEmails.Add(new ContactEmail()
							{
								Kind = kind,
								Address = address ?? string.Empty
							});
						}
						while (cursor.MoveToNext());
					}
					cursor.Close();
				}
			}
			return contactEmails;
		}

		private static IEnumerable<ContactAddress> GetAddresses(string[] idQuery, ContentResolver contentResolver)
		{
			var contactAddresss = new List<ContactAddress>();
			var uri = ContactsContract.CommonDataKinds.StructuredPostal.ContentUri?.BuildUpon()?.AppendQueryParameter(ContactsContract.RemoveDuplicateEntries, "1")?.Build();

			if (uri != null)
			{
				using ICursor? cursor = contentResolver.Query(
				   uri,
				   null,
				   ContactsContract.Contacts.InterfaceConsts.Id + "=?",
				   idQuery,
				   null);

				if (cursor != null)
				{
					if (cursor.MoveToFirst())
					{
						do
						{
							var city = cursor.GetString(cursor.GetColumnIndex(ContactsContract.CommonDataKinds.StructuredPostal.City));
							var country = cursor.GetString(cursor.GetColumnIndex(ContactsContract.CommonDataKinds.StructuredPostal.Country));
							var postalCode = cursor.GetString(cursor.GetColumnIndex(ContactsContract.CommonDataKinds.StructuredPostal.Postcode));
							var region = cursor.GetString(cursor.GetColumnIndex(ContactsContract.CommonDataKinds.StructuredPostal.Region));
							var street = cursor.GetString(cursor.GetColumnIndex(ContactsContract.CommonDataKinds.StructuredPostal.Street));

							var addressType = cursor.GetString(cursor.GetColumnIndex(ContactsContract.CommonDataKinds.StructuredPostal.InterfaceConsts.Type));
							var kind = TypeToContactAddressKind(addressType);

							contactAddresss.Add(new ContactAddress()
							{
								Kind = kind,
								Country = country ?? string.Empty,
								Locality = city ?? string.Empty,
								PostalCode = postalCode ?? string.Empty,
								Region = region ?? string.Empty,
								StreetAddress = street ?? string.Empty
							});
						}
						while (cursor.MoveToNext());
					}
					cursor.Close();
				}
			}
			return contactAddresss;
		}

		private static ContactPhoneKind TypeToContactPhoneKind(string? type)
		{
			if (int.TryParse(type, out var typeInt))
			{
				if (Enum.IsDefined(typeof(PhoneDataKind), typeInt))
				{
					var PhoneType = (PhoneDataKind)typeInt;
					return PhoneType switch
					{
						PhoneDataKind.Assistant => ContactPhoneKind.Assistant,
						PhoneDataKind.CompanyMain => ContactPhoneKind.Company,
						PhoneDataKind.Main => ContactPhoneKind.Mobile,
						PhoneDataKind.Mobile => ContactPhoneKind.Mobile,
						PhoneDataKind.Home => ContactPhoneKind.Home,
						PhoneDataKind.FaxHome => ContactPhoneKind.HomeFax,
						PhoneDataKind.Pager => ContactPhoneKind.Pager,
						PhoneDataKind.Radio => ContactPhoneKind.Radio,
						PhoneDataKind.Work => ContactPhoneKind.Work,
						PhoneDataKind.WorkMobile => ContactPhoneKind.Work,
						PhoneDataKind.WorkPager => ContactPhoneKind.Work,
						PhoneDataKind.FaxWork => ContactPhoneKind.BusinessFax,
						_ => ContactPhoneKind.Other
					};
				}
			}
			return ContactPhoneKind.Other;
		}

		private static ContactEmailKind TypeToContactEmailKind(string? type)
		{
			if (int.TryParse(type, out var typeInt))
			{
				if (Enum.IsDefined(typeof(EmailDataKind), typeInt))
				{
					var EmailType = (EmailDataKind)typeInt;
					return EmailType switch
					{
						EmailDataKind.Home => ContactEmailKind.Personal,
						EmailDataKind.Mobile => ContactEmailKind.Personal,
						EmailDataKind.Work => ContactEmailKind.Work,
						_ => ContactEmailKind.Other
					};
				}
			}
			return ContactEmailKind.Other;
		}

		private static ContactAddressKind TypeToContactAddressKind(string? type)
		{
			if (int.TryParse(type, out var typeInt))
			{
				if (Enum.IsDefined(typeof(AddressDataKind), typeInt))
				{
					var addressType = (AddressDataKind)typeInt;
					return addressType switch
					{
						AddressDataKind.Home => ContactAddressKind.Home,
						AddressDataKind.Work => ContactAddressKind.Work,
						_ => ContactAddressKind.Other
					};
				}
			}
			return ContactAddressKind.Other;
		}
	}
}
