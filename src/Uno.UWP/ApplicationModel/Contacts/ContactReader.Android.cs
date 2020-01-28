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
		internal ContactQueryOptions _queryOptions;
		private Android.Database.ICursor _cursor = null;
		private Android.Content.ContentResolver _contentResolver = null;

		internal ContactReader(ContactQueryOptions options)
		{
			if (options is null)
				throw new ArgumentNullException();

			_queryOptions = options;

			Android.Net.Uri oUri;
			string sColumnIdName = "_id";

			switch (options.SearchFields)
			{
				case ContactQuerySearchFields.Phone:
					oUri = Android.Net.Uri.WithAppendedPath(
						Android.Provider.ContactsContract.PhoneLookup.ContentFilterUri, // Phone.Contact_ID mapped to .Contacts._ID 
						Android.Net.Uri.Encode(options.SearchText));
					sColumnIdName = "contact_id";
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


			_contentResolver = Android.App.Application.Context.ContentResolver;

			_cursor = _contentResolver.Query(oUri,
									new string[] { sColumnIdName, "display_name" },  // which columns
									null,   // where
									null,   
									null);   // == date DESC
			if(!_cursor.MoveToFirst())
			{
				_cursor.Dispose();
				_cursor = null;
			}
		}

		~ContactReader()
		{
			if(_cursor != null)
			{
				_cursor.Dispose();
				_cursor = null;
			}
		}


		private async Task<ContactBatch> ReadBatchAsyncTask()
		{
			ContactBatch batch = new ContactBatch(ReadBatchInternal());
			return batch;
		}

		public IAsyncOperation<ContactBatch> ReadBatchAsync()
			=> ReadBatchAsyncTask().AsAsyncOperation();

		internal List<Contact> ReadBatchInternal()
		{
			var entriesList = new List<Contact>();

			if (_cursor is null)
			{
				return entriesList;
			}


			ContactQueryDesiredFields desiredFields = _queryOptions.DesiredFields;
			// default value (==None) treat as "all"
			if (desiredFields == ContactQueryDesiredFields.None)
				desiredFields = ContactQueryDesiredFields.EmailAddress | ContactQueryDesiredFields.PhoneNumber | ContactQueryDesiredFields.PostalAddress;

			// add fields we search by
			if (_queryOptions.SearchFields.HasFlag(ContactQuerySearchFields.Email))
				desiredFields |= ContactQueryDesiredFields.EmailAddress;
			if (_queryOptions.SearchFields.HasFlag(ContactQuerySearchFields.Phone))
				desiredFields |= ContactQueryDesiredFields.PhoneNumber;

			for (int pageGuard = 100; pageGuard > 0 ; pageGuard--)
			{
				var entry = new Contact();
				int contactId = _cursor.GetInt(0);  // we defined columns while opening cursor, so we know what data is in which columns

				entry.DisplayName = _cursor.GetString(1);   // we defined columns while opening cursor, so we know what data is in which columns

				bool searchFound = false; // should it be included in result set
				if (_queryOptions.SearchFields == ContactQuerySearchFields.None ||	// no filtering at all
					_queryOptions.SearchFields == ContactQuerySearchFields.Phone ||	// filtering done by Android
					_queryOptions.SearchFields == ContactQuerySearchFields.Name)      // filtering done by Android
							searchFound = true; // include in result - and skip tests

				if (!searchFound && _queryOptions.SearchFields.HasFlag(ContactQuerySearchFields.Name))
				{
					if (entry.DisplayName.Contains(_queryOptions.SearchText))
						searchFound = true;
				}



				// filling properties, using other tables


				// https://developer.android.com/reference/android/provider/ContactsContract.CommonDataKinds.Phone
				// NUMBER, TYPE
				entry.Phones.Clear();
				if (desiredFields.HasFlag(ContactQueryDesiredFields.PhoneNumber))
				{
					Android.Database.ICursor subCursor = _contentResolver.Query(
										Android.Provider.ContactsContract.Data.ContentUri,
										new string[] { "data1", "data2" }, //null,   // all columns
																		   // ContactsContract.Data.RAW_CONTACT_ID + " = ? AND " + ContactsContract.Data.MIMETYPE + " = ?",
										"contact_id = ? AND mimetype = ?",
										new string[] { contactId.ToString(), Android.Provider.ContactsContract.CommonDataKinds.Phone.ContentItemType },
										null);   // default order

					//int columnD1 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data1); // Phone.NUMBER
					//int columnD2 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data2); // Phone.TYPE

					for (int itemGuard = 10; itemGuard > 0 && subCursor.MoveToNext(); itemGuard--)
					{
						var itemEntry = new ContactPhone();
						itemEntry.Number = subCursor.GetString(0);   // we defined columns while opening cursor, so we know what data is in which columns

						if (!searchFound && _queryOptions.SearchFields.HasFlag(ContactQuerySearchFields.Phone))
						{
							if (itemEntry.Number.Contains(_queryOptions.SearchText))
								searchFound = true;
						}

						switch (subCursor.GetInt(1))    // we defined columns while opening cursor, so we know what data is in which columns
						{
							case 1:
								itemEntry.Kind = ContactPhoneKind.Home;
								break;
							case 2:
								itemEntry.Kind = ContactPhoneKind.Mobile;
								break;
							case 3:
								itemEntry.Kind = ContactPhoneKind.Work;
								break;
							case 6:
								itemEntry.Kind = ContactPhoneKind.Pager;
								break;
							case 4:
								itemEntry.Kind = ContactPhoneKind.BusinessFax;
								break;
							case 5:
								itemEntry.Kind = ContactPhoneKind.HomeFax;
								break;
							case 10:
								itemEntry.Kind = ContactPhoneKind.Company;
								break;
							case 19:
								itemEntry.Kind = ContactPhoneKind.Assistant;
								break;
							case 14:
								itemEntry.Kind = ContactPhoneKind.Radio;
								break;
							default:    // TYPE_CALLBACK, TYPE_CAR, TYPE_ISDN, TYPE_MAIN, TYPE_MMS, TYPE_OTHER, TYPE_OTHER_FAX, TYPE_PAGER, TYPE_TELEX, TYPE_TTY_TDD, TYPE_WORK_MOBILE, TYPE_WORK_PAGER
								itemEntry.Kind = ContactPhoneKind.Other;
								break;
						}
						entry.Phones.Add(itemEntry);

					}
					subCursor.Close();




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
					Android.Database.ICursor subCursor = _contentResolver.Query(
									Android.Provider.ContactsContract.Data.ContentUri,
									new string[] { "data1", "data2" }, //null,   // all columns
																	   // ContactsContract.Data.RAW_CONTACT_ID + " = ? AND " + ContactsContract.Data.MIMETYPE + " = ?",
									"contact_id = ? AND mimetype = ?",
									new string[] { contactId.ToString(), Android.Provider.ContactsContract.CommonDataKinds.Email.ContentItemType },
									null);   // default order

					//columnD1 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data1); // Email.ADDRESS
					//columnD2 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data2); // Email.TYPE
					for (int itemGuard = 10; itemGuard > 0 && subCursor.MoveToNext(); itemGuard--)
					{
						var itemEntry = new ContactEmail();
						itemEntry.Address = subCursor.GetString(0);     // we defined columns while opening cursor, so we know what data is in which columns
						if (!searchFound && _queryOptions.SearchFields.HasFlag(ContactQuerySearchFields.Email))
						{
							if (itemEntry.Address.Contains(_queryOptions.SearchText))
								searchFound = true;
						}

						switch (subCursor.GetInt(1))    // we defined columns while opening cursor, so we know what data is in which columns
						{
							case 1: // TYPE_HOME
								itemEntry.Kind = ContactEmailKind.Personal;
								break;
							case 2:
								itemEntry.Kind = ContactEmailKind.Work;
								break;
							default:    // TYPE_MOBILE, TYPE_OTHER
								itemEntry.Kind = ContactEmailKind.Other;
								break;
						}
						entry.Emails.Add(itemEntry);
					}
					subCursor.Close();

					if (!searchFound && _queryOptions.SearchFields == ContactQuerySearchFields.Email)
					{
						pageGuard++;    // as this item is not returned...
						continue;
					}
				}

				// https://developer.android.com/reference/android/provider/ContactsContract.CommonDataKinds.StructuredName
				// DISPLAY_NAME, GIVEN_NAME, FAMILY_NAME, PREFIX, MIDDLE_NAME, SUFFIX, PHONETIC_GIVEN_NAME, PHONETIC_MIDDLE_NAME, PHONETIC_FAMILY_NAME
				{
					Android.Database.ICursor subCursor = _contentResolver.Query(
										Android.Provider.ContactsContract.Data.ContentUri,
										new string[] { "data1", "data2", "data3", "data4", "data5", "data6" }, //null,   // all columns
																											   // ContactsContract.Data.RAW_CONTACT_ID + " = ? AND " + ContactsContract.Data.MIMETYPE + " = ?",
										"contact_id = ? AND mimetype = ?",
										new string[] { contactId.ToString(), Android.Provider.ContactsContract.CommonDataKinds.StructuredName.ContentItemType },
										null);   // default order

					if (subCursor.MoveToNext())
					{

						// we defined columns while opening cursor, so we know what data is in which columns
						entry.MiddleName = subCursor.GetString(4);       
						entry.LastName = subCursor.GetString(2); 
						entry.FirstName = subCursor.GetString(1);
						entry.HonorificNamePrefix = subCursor.GetString(3);
						entry.HonorificNameSuffix = subCursor.GetString(5);
						entry.DisplayNameOverride = subCursor.GetString(0);

						if (!searchFound && _queryOptions.SearchFields.HasFlag(ContactQuerySearchFields.Name))
						{
							if (entry.MiddleName.Contains(_queryOptions.SearchText) ||
									entry.LastName.Contains(_queryOptions.SearchText) ||
									entry.FirstName.Contains(_queryOptions.SearchText) ||
									entry.HonorificNamePrefix.Contains(_queryOptions.SearchText) ||
									entry.HonorificNameSuffix.Contains(_queryOptions.SearchText) ||
									entry.DisplayName.Contains(_queryOptions.SearchText))
								searchFound = true;
						}


					}
					subCursor.Close();

					if (!searchFound && _queryOptions.SearchFields == ContactQuerySearchFields.Name)
					{
						pageGuard++;    // as this item is not returned...
						continue;
					}
				}

				//// https://developer.android.com/reference/android/provider/ContactsContract.CommonDataKinds.StructuredPostal
				//// 	FORMATTED_ADDRESS, TYPE, LABEL, STREET, POBOX, NEIGHBORHOOD, CITY, REGION, POSTCODE, COUNTRY

				entry.Addresses.Clear();
				if (desiredFields.HasFlag(ContactQueryDesiredFields.PostalAddress))
				{
					Android.Database.ICursor subCursor = _contentResolver.Query(
									Android.Provider.ContactsContract.Data.ContentUri,
									new string[] { "data2", "data4", "data7", "data8", "data9", "data10" }, //null,// all columns
																											// ContactsContract.Data.RAW_CONTACT_ID + " = ? AND " + ContactsContract.Data.MIMETYPE + " = ?",
									"raw_contact_id = ? AND mimetype = ?",
									new string[] { contactId.ToString(), Android.Provider.ContactsContract.CommonDataKinds.StructuredPostal.ContentItemType },
									null);   // default order

					for (int itemGuard = 10; itemGuard > 0 && subCursor.MoveToNext(); itemGuard--)
					{
						var itemEntry = new ContactAddress();
						itemEntry.StreetAddress = subCursor.GetString(1);    // we defined columns while opening cursor, so we know what data is in which columns
						itemEntry.Region = subCursor.GetString(3);
						itemEntry.PostalCode = subCursor.GetString(4);
						itemEntry.Locality = subCursor.GetString(2);
						//itemEntry.Description = subCursor.GetString(columnD4);
						itemEntry.Country = subCursor.GetString(5);

						if (!searchFound && _queryOptions.SearchFields == ContactQuerySearchFields.All)
						{
							if (itemEntry.StreetAddress.Contains(_queryOptions.SearchText) ||
									itemEntry.Region.Contains(_queryOptions.SearchText) ||
									itemEntry.PostalCode.Contains(_queryOptions.SearchText) ||
									itemEntry.Locality.Contains(_queryOptions.SearchText) ||
									itemEntry.Country.Contains(_queryOptions.SearchText))
								searchFound = true;
						}



						switch (subCursor.GetInt(0))    // we defined columns while opening cursor, so we know what data is in which columns
						{
							case 1: // TYPE_HOME
								itemEntry.Kind = ContactAddressKind.Home;
								break;
							case 2:
								itemEntry.Kind = ContactAddressKind.Work;
								break;
							default:    // TYPE_OTHER
								itemEntry.Kind = ContactAddressKind.Other;
								break;
						}

						entry.Addresses.Add(itemEntry);
					}
					subCursor.Close();

					if (!searchFound && _queryOptions.SearchFields != ContactQuerySearchFields.None)
					{
						pageGuard++;    // as this item is not returned...
						continue;
					}
				}

				entriesList.Add(entry);

				if(!_cursor.MoveToNext())
				{
					_cursor.Close();
					_cursor.Dispose();
					_cursor = null;

					break;
				}
			}

			return entriesList;
		}
	}
}

#endif
