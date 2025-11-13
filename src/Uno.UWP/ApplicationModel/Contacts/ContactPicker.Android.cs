#nullable enable

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Database;
using Android.Provider;
using Uno.Helpers.Activities;
using Uno.UI;
using Windows.Extensions;

namespace Windows.ApplicationModel.Contacts
{
	public partial class ContactPicker
	{
		private static Task<bool> IsSupportedTaskAsync(CancellationToken token) => Task.FromResult(true);

		private async Task<Contact[]> PickContactsAsync(bool multiple, CancellationToken token)
		{
			if (!await PermissionsHelper.CheckReadContactsPermission(default))
			{
				if (!await PermissionsHelper.TryGetReadContactsPermission(default))
				{
					throw new InvalidOperationException("android.permission.READ_CONTACTS permission is required");
				}
			}

			using var intent = new Intent(Intent.ActionPick);
			intent.SetType(ContactsContract.CommonDataKinds.Phone.ContentType);
			var activity = await AwaitableResultActivity.StartAsync();
			var result = await activity.StartActivityForResultAsync(intent);

			if (token.IsCancellationRequested)
			{
				return Array.Empty<Contact>();
			}

			if (result?.Intent?.Data is Android.Net.Uri contactUri)
			{
				using var contentResolver = Application.Context.ContentResolver;
				if (contentResolver != null)
				{
					var projection = new string[] { "lookup" };
					using ICursor? cursorLookUpKey = contentResolver.Query(contactUri, projection, null, null, null);
					if (cursorLookUpKey?.MoveToFirst() == true)
					{
						var lookupKey = cursorLookUpKey.GetString(cursorLookUpKey.GetColumnIndex(projection[0]));
						if (lookupKey != null)
						{
							return new[] { LookupToContact(lookupKey, contentResolver) };
						}
					}
				}
			}
			return Array.Empty<Contact>();
		}

		private static Contact LookupToContact(string lookupKey, ContentResolver contentResolver)
		{
			var contact = new Contact();

			ReadStructuredName(contact, lookupKey, contentResolver);
			ReadNickname(contact, lookupKey, contentResolver);
			ReadNotes(contact, lookupKey, contentResolver);
			ReadPhones(contact, lookupKey, contentResolver);
			ReadEmails(contact, lookupKey, contentResolver);
			ReadAddresses(contact, lookupKey, contentResolver);

			return contact;
		}

		private static void ReadStructuredName(Contact contact, string lookupKey, ContentResolver contentResolver)
		{
			var contactWhere = "lookup = ? AND " + "mimetype = ?";
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
					contact.HonorificNamePrefix = GetColumn(cursor, ContactsContract.CommonDataKinds.StructuredName.Prefix);
					contact.FirstName = GetColumn(cursor, ContactsContract.CommonDataKinds.StructuredName.GivenName);
					contact.MiddleName = GetColumn(cursor, ContactsContract.CommonDataKinds.StructuredName.MiddleName);
					contact.LastName = GetColumn(cursor, ContactsContract.CommonDataKinds.StructuredName.FamilyName);
					contact.HonorificNameSuffix = GetColumn(cursor, ContactsContract.CommonDataKinds.StructuredName.Suffix);

					contact.YomiGivenName = GetColumn(cursor, ContactsContract.CommonDataKinds.StructuredName.PhoneticGivenName);
					contact.YomiFamilyName = GetColumn(cursor, ContactsContract.CommonDataKinds.StructuredName.PhoneticFamilyName);
				}
			}
		}

		private static void ReadNickname(Contact contact, string lookupKey, ContentResolver contentResolver)
		{
			var nicknameWhere =
			   "lookup = ? AND " +
			   "mimetype = ?";

			var nicknameWhereParams = new[]
			{
				lookupKey,
				ContactsContract.CommonDataKinds.Nickname.ContentItemType
			};

			if (ContactsContract.Data.ContentUri != null)
			{
				using var noteCursor = contentResolver.Query(
					ContactsContract.Data.ContentUri,
					null,
					nicknameWhere,
					nicknameWhereParams,
					null
				);
				if (noteCursor?.MoveToFirst() == true)
				{
					contact.Nickname = GetColumn(noteCursor, ContactsContract.CommonDataKinds.Nickname.Name);
				}
			}
		}

		private static void ReadNotes(Contact contact, string lookupKey, ContentResolver contentResolver)
		{
			var noteWhere =
				"lookup = ? AND " +
				"mimetype = ?";

			var noteWhereParams = new[]
			{
				lookupKey,
				ContactsContract.CommonDataKinds.Note.ContentItemType
			};

			if (ContactsContract.Data.ContentUri != null)
			{
				using var noteCursor = contentResolver.Query(
					ContactsContract.Data.ContentUri,
					null,
					noteWhere,
					noteWhereParams,
					null
				);
				if (noteCursor?.MoveToFirst() == true)
				{
					contact.Notes = GetColumn(noteCursor, ContactsContract.CommonDataKinds.Note.NoteColumnId);
				}
			}
		}

		private static void ReadPhones(Contact contact, string lookupKey, ContentResolver contentResolver)
		{
			var phonesWhere = "lookup = ?";
			var phonesWhereParams = new[]
			{
				lookupKey
			};

			var uri = ContactsContract.CommonDataKinds.Phone.ContentUri?
				.BuildUpon()?
				.AppendQueryParameter(ContactsContract.RemoveDuplicateEntries, "1")?
				.Build();

			if (uri != null)
			{
				using ICursor? cursor = contentResolver.Query(
				   uri,
				   null,
				   phonesWhere,
				   phonesWhereParams,
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

							contact.Phones.Add(new ContactPhone()
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
		}

		private static void ReadEmails(Contact contact, string lookupKey, ContentResolver contentResolver)
		{
			var emailsWhere = "lookup = ?";
			var emailsWhereParams = new[]
			{
				lookupKey
			};

			var uri = ContactsContract.CommonDataKinds.Email.ContentUri?
				.BuildUpon()?
				.AppendQueryParameter(ContactsContract.RemoveDuplicateEntries, "1")?
				.Build();

			if (uri != null)
			{
				using ICursor? cursor = contentResolver.Query(
				   uri,
				   null,
				   emailsWhere,
				   emailsWhereParams,
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

							contact.Emails.Add(new ContactEmail()
							{
								Kind = kind,
								Address = address ?? string.Empty
							});
						}
						while (cursor.MoveToNext());
					}
				}
			}
		}

		private static void ReadAddresses(Contact contact, string lookupKey, ContentResolver contentResolver)
		{
			var addressesWhere = "lookup = ?";
			var addressesWhereParams = new[]
			{
				lookupKey
			};

			var uri = ContactsContract.CommonDataKinds.StructuredPostal.ContentUri?
				.BuildUpon()?
				.AppendQueryParameter(ContactsContract.RemoveDuplicateEntries, "1")?
				.Build();

			if (uri != null)
			{
				using ICursor? cursor = contentResolver.Query(
				   uri,
				   null,
				   addressesWhere,
				   addressesWhereParams,
				   null);

				if (cursor != null)
				{
					if (cursor.MoveToFirst())
					{
						do
						{
							var city = GetColumn(cursor, ContactsContract.CommonDataKinds.StructuredPostal.City);
							var country = GetColumn(cursor, ContactsContract.CommonDataKinds.StructuredPostal.Country);
							var postalCode = GetColumn(cursor, ContactsContract.CommonDataKinds.StructuredPostal.Postcode);
							var region = GetColumn(cursor, ContactsContract.CommonDataKinds.StructuredPostal.Region);
							var street = GetColumn(cursor, ContactsContract.CommonDataKinds.StructuredPostal.Street);

							var addressType = GetColumn(cursor, ContactsContract.CommonDataKinds.StructuredPostal.InterfaceConsts.Type);
							var kind = TypeToContactAddressKind(addressType);

							contact.Addresses.Add(new ContactAddress()
							{
								Kind = kind,
								Country = country,
								Locality = city,
								PostalCode = postalCode,
								Region = region,
								StreetAddress = street
							});
						}
						while (cursor.MoveToNext());
					}
					cursor.Close();
				}
			}
		}

		private static ContactPhoneKind TypeToContactPhoneKind(string? type)
		{
			if (int.TryParse(type, CultureInfo.InvariantCulture, out var typeInt))
			{
				if (Enum.IsDefined((PhoneDataKind)typeInt))
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
			if (int.TryParse(type, CultureInfo.InvariantCulture, out var typeInt))
			{
				if (Enum.IsDefined((EmailDataKind)typeInt))
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
			if (int.TryParse(type, CultureInfo.InvariantCulture, out var typeInt))
			{
				if (Enum.IsDefined((AddressDataKind)typeInt))
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

		private static string GetColumn(ICursor cursor, string? columnName)
		{
			if (columnName == null)
			{
				return string.Empty;
			}

			var columnIndex = cursor.GetColumnIndex(columnName);
			return cursor.GetString(columnIndex) ?? string.Empty;
		}
	}
}
