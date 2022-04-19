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

			switch (options.TextSearch.Fields)
			{
				case ContactQuerySearchFields.Phone:
					oUri = Android.Net.Uri.WithAppendedPath(
						Android.Provider.ContactsContract.PhoneLookup.ContentFilterUri, // Phone.Contact_ID mapped to .Contacts._ID 
						Android.Net.Uri.Encode(options.TextSearch.Text));
					columnIdName = "contact_id";
					break;
				case ContactQuerySearchFields.Name:
					oUri = Android.Net.Uri.WithAppendedPath(
						Android.Provider.ContactsContract.Contacts.ContentFilterUri,
						Android.Net.Uri.Encode(options.TextSearch.Text));
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
			var desiredFields = CanonizeDesiredFields(_queryOptions.DesiredFields, _queryOptions.TextSearch.Fields);

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
				if (_queryOptions.TextSearch.Fields == ContactQuerySearchFields.None ||  // no filtering at all
					_queryOptions.TextSearch.Fields == ContactQuerySearchFields.Phone || // filtering done by Android
					_queryOptions.TextSearch.Fields == ContactQuerySearchFields.Name)      // filtering done by Android
				{
					searchFound = true; // include in result - and skip tests
				}

				if (!searchFound && _queryOptions.TextSearch.Fields.HasFlag(ContactQuerySearchFields.Name))
				{
					if (entry.DisplayNameOverride.Contains(_queryOptions.TextSearch.Text))
					{
						searchFound = true;
					}
				}


				// filling properties, using other tables

				// https://developer.android.com/reference/android/provider/ContactsContract.CommonDataKinds.Phone
				entry.Phones.Clear();
				if (desiredFields.HasFlag(ContactQueryDesiredFields.PhoneNumber))
				{
					searchFound |= ReadContactPhones(entry, contactId, _contentResolver);   // we tested _contentResolver in  constructor; and if it is null, exception was thrown

					if (!searchFound && _queryOptions.TextSearch.Fields == ContactQuerySearchFields.Phone)
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
					searchFound |= ReadContactEmail(entry, contactId, _contentResolver);   // we tested _contentResolver in  constructor; and if it is null, exception was thrown

					if (!searchFound && _queryOptions.TextSearch.Fields == ContactQuerySearchFields.Email)
					{
						pageGuard++;    // as this item is not returned...
						continue;
					}
				}

				// https://developer.android.com/reference/android/provider/ContactsContract.CommonDataKinds.StructuredName
				// DISPLAY_NAME, GIVEN_NAME, FAMILY_NAME, PREFIX, MIDDLE_NAME, SUFFIX, PHONETIC_GIVEN_NAME, PHONETIC_MIDDLE_NAME, PHONETIC_FAMILY_NAME

				searchFound |= ReadContactStructuredName(entry, contactId, _contentResolver!);   // we tested _contentResolver in  constructor; and if it is null, exception was thrown

				if (!searchFound && _queryOptions.TextSearch.Fields == ContactQuerySearchFields.Name)
				{
					pageGuard++;    // as this item is not returned...
					continue;
				}

				//// https://developer.android.com/reference/android/provider/ContactsContract.CommonDataKinds.StructuredPostal
				//// 	FORMATTED_ADDRESS, TYPE, LABEL, STREET, POBOX, NEIGHBORHOOD, CITY, REGION, POSTCODE, COUNTRY

				entry.Addresses.Clear();
				if (desiredFields.HasFlag(ContactQueryDesiredFields.PostalAddress))
				{

					searchFound |= ReadContactAddresses(entry, contactId, _contentResolver);   // we tested _contentResolver in  constructor; and if it is null, exception was thrown

					if (!searchFound && _queryOptions.TextSearch.Fields != ContactQuerySearchFields.None)
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


		private bool ReadContactPhones(Contact entry, int contactId, Android.Content.ContentResolver contentResolver)
		{
			var contentUri = Android.Provider.ContactsContract.Data.ContentUri;
			if (contentUri is null)
			{
				throw new NullReferenceException("Windows.ApplicationModel.Contacts.ContactReader.ReadBatchInternal, ContentUri is null (impossible)");
			}

			using var subCursor = contentResolver?.Query(
								contentUri,
								new string[] { "data1", "data2" }, //null,   // all columns
																   // ContactsContract.Data.RAW_CONTACT_ID + " = ? AND " + ContactsContract.Data.MIMETYPE + " = ?",
								"contact_id = ? AND mimetype = ?",
								new string[] { contactId.ToString(), Android.Provider.ContactsContract.CommonDataKinds.Phone.ContentItemType },
								null);   // default order
			if (subCursor is null)
			{
				throw new NullReferenceException("Windows.ApplicationModel.Contacts.ContactReader.ReadBatchInternal, subCursor is null (impossible)");
			}

			//int columnD1 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data1); // Phone.NUMBER
			//int columnD2 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data2); // Phone.TYPE

			bool searchFound = false;

			for (int itemGuard = 10; itemGuard > 0 && subCursor.MoveToNext(); itemGuard--)
			{
				var itemEntry = new ContactPhone();
				itemEntry.Number = subCursor.GetString(0) ?? ""; // we defined columns while opening cursor, so we know what data is in which columns

				// we defined columns while opening cursor, so we know what data is in which columns
				itemEntry.Kind = ContactPicker.TypeToContactPhoneKind(subCursor.GetInt(1));
				entry.Phones.Add(itemEntry);

				if (!searchFound && _queryOptions.TextSearch.Fields.HasFlag(ContactQuerySearchFields.Phone))
				{
					// removing spaces, i.e. "601 4746" == "6014746"
					if (itemEntry.Number.Replace(" ","").Contains(_queryOptions.TextSearch.Text.Replace(" ","")))
					{
						searchFound = true;
					}
				}
			}
			subCursor.Close();
			return searchFound;
		}

		private bool ReadContactEmail(Contact entry, int contactId, Android.Content.ContentResolver contentResolver)
		{
			var contentUri = Android.Provider.ContactsContract.Data.ContentUri;
			if (contentUri is null)
			{
				throw new NullReferenceException("Windows.ApplicationModel.Contacts.ContactReader.ReadBatchInternal, ContentUri2 is null (impossible)");
			}

			using var subCursor = _contentResolver?.Query(
							contentUri,
							new string[] { "data1", "data2" }, //null,   // all columns
															   // ContactsContract.Data.RAW_CONTACT_ID + " = ? AND " + ContactsContract.Data.MIMETYPE + " = ?",
							"contact_id = ? AND mimetype = ?",
							new string[] { contactId.ToString(), Android.Provider.ContactsContract.CommonDataKinds.Email.ContentItemType },
							null);   // default order
			if (subCursor is null)
			{
				throw new NullReferenceException("Windows.ApplicationModel.Contacts.ContactReader.ReadBatchInternal, subCursor2 is null (impossible)");
			}


			bool searchFound = false;

			//columnD1 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data1); // Email.ADDRESS
			//columnD2 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data2); // Email.TYPE
			for (int itemGuard = 10; itemGuard > 0 && subCursor.MoveToNext(); itemGuard--)
			{
				var itemEntry = new ContactEmail();
				itemEntry.Address = subCursor.GetString(0) ?? "";     // we defined columns while opening cursor, so we know what data is in which columns
				itemEntry.Kind = ContactPicker.TypeToContactEmailKind(subCursor.GetInt(1));

				entry.Emails.Add(itemEntry);

				if (!searchFound && _queryOptions.TextSearch.Fields.HasFlag(ContactQuerySearchFields.Email))
				{
					if (itemEntry.Address.Contains(_queryOptions.TextSearch.Text))
					{
						searchFound = true;
					}
				}

			}
			subCursor.Close();
			return searchFound;

		}

		private bool ReadContactStructuredName(Contact entry, int contactId, Android.Content.ContentResolver contentResolver)
		{
			var contentUri = Android.Provider.ContactsContract.Data.ContentUri;
			if (contentUri is null)
			{
				throw new NullReferenceException("Windows.ApplicationModel.Contacts.ContactReader.ReadBatchInternal, ContentUri2 is null (impossible)");
			}

			using var subCursor = _contentResolver?.Query(
								contentUri,
								new string[] { "data1", "data2", "data3", "data4", "data5", "data6", "data7", "data9" }, //null,   // all columns
																									   // ContactsContract.Data.RAW_CONTACT_ID + " = ? AND " + ContactsContract.Data.MIMETYPE + " = ?",
								"contact_id = ? AND mimetype = ?",
								new string[] { contactId.ToString(), Android.Provider.ContactsContract.CommonDataKinds.StructuredName.ContentItemType },
								null);   // default order
			if (subCursor is null)
			{
				throw new NullReferenceException("Windows.ApplicationModel.Contacts.ContactReader.ReadBatchInternal, subCursor2 is null (impossible)");
			}

			bool searchFound = false;

			if (subCursor.MoveToFirst())
			{
				// we defined columns while opening cursor, so we know what data is in which columns
				entry.MiddleName = subCursor.GetString(4) ?? "";
				entry.LastName = subCursor.GetString(2) ?? "";
				entry.FirstName = subCursor.GetString(1) ?? "";
				entry.HonorificNamePrefix = subCursor.GetString(3) ?? "";
				entry.HonorificNameSuffix = subCursor.GetString(5) ?? "";
				entry.DisplayNameOverride = subCursor.GetString(0) ?? "";

				entry.YomiGivenName = subCursor.GetString(6) ?? "";
				entry.YomiFamilyName = subCursor.GetString(7) ?? "";

				if (!searchFound && _queryOptions.TextSearch.Fields.HasFlag(ContactQuerySearchFields.Name))
				{
					if (entry.MiddleName.Contains(_queryOptions.TextSearch.Text, StringComparison.OrdinalIgnoreCase) ||
							entry.LastName.Contains(_queryOptions.TextSearch.Text, StringComparison.OrdinalIgnoreCase) ||
							entry.FirstName.Contains(_queryOptions.TextSearch.Text, StringComparison.OrdinalIgnoreCase) ||
							entry.HonorificNamePrefix.Contains(_queryOptions.TextSearch.Text, StringComparison.OrdinalIgnoreCase) ||
							entry.HonorificNameSuffix.Contains(_queryOptions.TextSearch.Text, StringComparison.OrdinalIgnoreCase) ||
							entry.DisplayName.Contains(_queryOptions.TextSearch.Text, StringComparison.OrdinalIgnoreCase) ||
							entry.YomiGivenName.Contains(_queryOptions.TextSearch.Text, StringComparison.OrdinalIgnoreCase) ||
							entry.YomiFamilyName.Contains(_queryOptions.TextSearch.Text, StringComparison.OrdinalIgnoreCase))

					{
						searchFound = true;
					}
				}


			}
			subCursor.Close();
			return searchFound;
		}

		private bool ReadContactAddresses(Contact entry, int contactId, Android.Content.ContentResolver contentResolver)
		{
			var contentUri = Android.Provider.ContactsContract.Data.ContentUri;
			if (contentUri is null)
			{
				throw new NullReferenceException("Windows.ApplicationModel.Contacts.ContactReader.ReadBatchInternal, ContentUri3 is null (impossible)");
			}

			var subCursor = _contentResolver?.Query(
							contentUri,
							new string[] { "data2", "data4", "data7", "data8", "data9", "data10" }, //null,// all columns
																									// ContactsContract.Data.RAW_CONTACT_ID + " = ? AND " + ContactsContract.Data.MIMETYPE + " = ?",
							"raw_contact_id = ? AND mimetype = ?",
							new string[] { contactId.ToString(), Android.Provider.ContactsContract.CommonDataKinds.StructuredPostal.ContentItemType },
							null);   // default order
			if (subCursor is null)
			{
				throw new NullReferenceException("Windows.ApplicationModel.Contacts.ContactReader.ReadBatchInternal, subCursor3 is null (impossible)");
			}

			bool searchFound = false;

			for (int itemGuard = 10; itemGuard > 0 && subCursor.MoveToNext(); itemGuard--)
			{
				var itemEntry = new ContactAddress();
				itemEntry.StreetAddress = subCursor.GetString(1) ?? "";    // we defined columns while opening cursor, so we know what data is in which columns
				itemEntry.Region = subCursor.GetString(3) ?? "";
				itemEntry.PostalCode = subCursor.GetString(4) ?? "";
				itemEntry.Locality = subCursor.GetString(2) ?? "";
				//itemEntry.Description = subCursor.GetString(columnD4);
				itemEntry.Country = subCursor.GetString(5) ?? "";

				itemEntry.Kind = ContactPicker.TypeToContactAddressKind(subCursor.GetInt(0));

				if (!searchFound && _queryOptions.TextSearch.Fields == ContactQuerySearchFields.All)
				{
					if (itemEntry.StreetAddress.Contains(_queryOptions.TextSearch.Text, StringComparison.OrdinalIgnoreCase) ||
							itemEntry.Region.Contains(_queryOptions.TextSearch.Text, StringComparison.OrdinalIgnoreCase) ||
							itemEntry.PostalCode.Contains(_queryOptions.TextSearch.Text, StringComparison.OrdinalIgnoreCase) ||
							itemEntry.Locality.Contains(_queryOptions.TextSearch.Text, StringComparison.OrdinalIgnoreCase) ||
							itemEntry.Country.Contains(_queryOptions.TextSearch.Text, StringComparison.OrdinalIgnoreCase))
					{
						searchFound = true;
					}
				}

				entry.Addresses.Add(itemEntry);
			}
			subCursor.Close();
			return searchFound;
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
