#nullable enable 

#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Windows.ApplicationModel.Contacts
{
	public partial class ContactReader
	{
		private ContactQueryOptions _queryOptions;
		private Android.Database.ICursor? _cursor = null;
		private Android.Content.ContentResolver _contentResolver;

		internal ContactReader(ContactQueryOptions options)
		{
			if (options is null)
				throw new ArgumentNullException();

			_queryOptions = options;

			Android.Net.Uri? oUri;
			string columnIdName = "_id";

			switch (options.SearchFields)
			{
				case ContactQuerySearchFields.Phone:
					oUri = Android.Net.Uri.WithAppendedPath(
						Android.Provider.ContactsContract.PhoneLookup.ContentFilterUri, // Phone.Contact_ID mapped to .Contacts._ID 
						Android.Net.Uri.Encode(options.SearchText));
					columnIdName = "contact_id";
					break;
				case ContactQuerySearchFields.Name:
					oUri = Android.Net.Uri.WithAppendedPath(
						Android.Provider.ContactsContract.Contacts.ContentFilterUri,
						Android.Net.Uri.Encode(options.SearchText));
					break;
				default:
					oUri = Android.Provider.ContactsContract.Contacts.ContentUri;
					break;
			}

			if (oUri is null)
			{
				throw new NullReferenceException("Windows.ApplicationModel.Contacts.ContactReader.ctor, oUri is null (impossible)");
			}

			var tempContentResolver = Android.App.Application.Context.ContentResolver;
			if (tempContentResolver is null)
			{
				throw new NullReferenceException("Windows.ApplicationModel.Contacts.ContactReader.ctor, ContentResolver is null (impossible)");
			}
			_contentResolver = tempContentResolver;

			_cursor = _contentResolver.Query(oUri,
								new string[] { columnIdName, "display_name" },  // which columns
								null,   // where
								null,
								null);   // == date DESC
			if (_cursor is null)
			{
				throw new NullReferenceException("Windows.ApplicationModel.Contacts.ContactReader.ctor, _cursor is null (impossible)");
			}

			if (!_cursor.MoveToFirst())
			{
				_cursor.Dispose();
				_cursor = null;
			}
		}

		~ContactReader()
		{
			if (_cursor != null)
			{
				_cursor.Dispose();
				_cursor = null;
			}
		}


		private async Task<ContactBatch> ReadBatchAsyncTask()
		{
			return new ContactBatch(ReadBatchInternal());
		}

		public IAsyncOperation<ContactBatch> ReadBatchAsync()
			=> ReadBatchAsyncTask().AsAsyncOperation();

		private List<Contact> ReadBatchInternal()
		{
			var entriesList = new List<Contact>();

			if (_cursor is null)
			{
				return entriesList;
			}


			// add fields we search by
			var desiredFields = CanonizeDesiredFields(_queryOptions.DesiredFields, _queryOptions.SearchFields);

			for (int pageGuard = 100; pageGuard > 0; pageGuard--)
			{
				var entry = new Contact();
				int contactId = _cursor.GetInt(0);  // we defined columns while opening cursor, so we know what data is in which columns

				var tempDisplayNameOverride = _cursor.GetString(1);
				if (tempDisplayNameOverride is null)
				{
					throw new NullReferenceException("Windows.ApplicationModel.Contacts.ContactReader.ReadBatchInternal, DisplayNameOverride is null (impossible)");
				}

				entry.DisplayNameOverride = tempDisplayNameOverride;   // we defined columns while opening cursor, so we know what data is in which columns

				bool searchFound = false; // should it be included in result set
				if (_queryOptions.SearchFields == ContactQuerySearchFields.None ||  // no filtering at all
					_queryOptions.SearchFields == ContactQuerySearchFields.Phone || // filtering done by Android
					_queryOptions.SearchFields == ContactQuerySearchFields.Name)      // filtering done by Android
				{
					searchFound = true; // include in result - and skip tests
				}

				if (!searchFound && _queryOptions.SearchFields.HasFlag(ContactQuerySearchFields.Name))
				{
					if (entry.DisplayNameOverride.Contains(_queryOptions.SearchText))
					{
						searchFound = true;
					}
				}


				// filling properties, using other tables

				// https://developer.android.com/reference/android/provider/ContactsContract.CommonDataKinds.Phone
				entry.Phones.Clear();
				if (desiredFields.HasFlag(ContactQueryDesiredFields.PhoneNumber))
				{
					ContactPicker.ReadPhones(entry, contactId.ToString(), _contentResolver!);   // we tested _contentResolver in  constructor; and if it is null, exception was thrown

					if (!searchFound && _queryOptions.SearchFields.HasFlag(ContactQuerySearchFields.Phone))
					{
						foreach (var phone in entry.Phones)
						{
							if (phone.Number.Contains(_queryOptions.SearchText))
							{
								searchFound = true;
							}
						}
					}

					if (!searchFound && _queryOptions.SearchFields == ContactQuerySearchFields.Phone)
					{
						pageGuard++;    // as this item is not returned...
						continue;
					}
				}

				// https://developer.android.com/reference/android/provider/ContactsContract.CommonDataKinds.Email
				// ADDRESS, TYPE
				entry.Emails.Clear();

				if (desiredFields.HasFlag(ContactQueryDesiredFields.EmailAddress))
				{
					ContactPicker.ReadEmails(entry, contactId.ToString(), _contentResolver!);   // we tested _contentResolver in  constructor; and if it is null, exception was thrown

					if (!searchFound && _queryOptions.SearchFields.HasFlag(ContactQuerySearchFields.Email))
					{
						foreach (var email in entry.Emails)
						{
							if (email.Address.Contains(_queryOptions.SearchText))
							{
								searchFound = true;
							}
						}
					}

					if (!searchFound && _queryOptions.SearchFields == ContactQuerySearchFields.Email)
					{
						pageGuard++;    // as this item is not returned...
						continue;
					}
				}

				// https://developer.android.com/reference/android/provider/ContactsContract.CommonDataKinds.StructuredName
				// DISPLAY_NAME, GIVEN_NAME, FAMILY_NAME, PREFIX, MIDDLE_NAME, SUFFIX, PHONETIC_GIVEN_NAME, PHONETIC_MIDDLE_NAME, PHONETIC_FAMILY_NAME

				ContactPicker.ReadStructuredName(entry, contactId.ToString(), _contentResolver!);   // we tested _contentResolver in  constructor; and if it is null, exception was thrown

				if (!searchFound && _queryOptions.SearchFields.HasFlag(ContactQuerySearchFields.Name))
				{
					if (entry.MiddleName.Contains(_queryOptions.SearchText) ||
							entry.LastName.Contains(_queryOptions.SearchText) ||
							entry.FirstName.Contains(_queryOptions.SearchText) ||
							entry.HonorificNamePrefix.Contains(_queryOptions.SearchText) ||
							entry.HonorificNameSuffix.Contains(_queryOptions.SearchText) ||
							entry.DisplayName.Contains(_queryOptions.SearchText))
					{
						searchFound = true;
					}
				}

				if (!searchFound && _queryOptions.SearchFields == ContactQuerySearchFields.Name)
				{
					pageGuard++;    // as this item is not returned...
					continue;
				}

				//// https://developer.android.com/reference/android/provider/ContactsContract.CommonDataKinds.StructuredPostal
				//// 	FORMATTED_ADDRESS, TYPE, LABEL, STREET, POBOX, NEIGHBORHOOD, CITY, REGION, POSTCODE, COUNTRY

				entry.Addresses.Clear();
				if (desiredFields.HasFlag(ContactQueryDesiredFields.PostalAddress))
				{

					ContactPicker.ReadAddresses(entry, contactId.ToString(), _contentResolver!);   // we tested _contentResolver in  constructor; and if it is null, exception was thrown

					if (!searchFound && _queryOptions.SearchFields == ContactQuerySearchFields.All)
					{
						foreach (var itemEntry in entry.Addresses)
						{
							if (itemEntry.StreetAddress.Contains(_queryOptions.SearchText) ||
									itemEntry.Region.Contains(_queryOptions.SearchText) ||
									itemEntry.PostalCode.Contains(_queryOptions.SearchText) ||
									itemEntry.Locality.Contains(_queryOptions.SearchText) ||
									itemEntry.Country.Contains(_queryOptions.SearchText))
							{
								searchFound = true;
							}
						}
					}

					if (!searchFound && _queryOptions.SearchFields != ContactQuerySearchFields.None)
					{
						pageGuard++;    // as this item is not returned...
						continue;
					}
				}

				entriesList.Add(entry);

				if (!_cursor.MoveToNext())
				{
					_cursor.Close();
					_cursor.Dispose();
					_cursor = null;

					break;
				}
			}

			return entriesList;
		}

		private ContactQueryDesiredFields CanonizeDesiredFields(ContactQueryDesiredFields queryDesiredFields, ContactQuerySearchFields searchFields)
		{
			var desiredFields = queryDesiredFields;
			// default value (==None) treat as "all"
			if (desiredFields == ContactQueryDesiredFields.None)
			{
				desiredFields = ContactQueryDesiredFields.EmailAddress | ContactQueryDesiredFields.PhoneNumber | ContactQueryDesiredFields.PostalAddress;
			}

			// add fields we search by
			if (searchFields.HasFlag(ContactQuerySearchFields.Email))
			{
				desiredFields |= ContactQueryDesiredFields.EmailAddress;
			}
			if (searchFields.HasFlag(ContactQuerySearchFields.Phone))
			{
				desiredFields |= ContactQueryDesiredFields.PhoneNumber;
			}

			return desiredFields;
		}

	}
}

#endif
