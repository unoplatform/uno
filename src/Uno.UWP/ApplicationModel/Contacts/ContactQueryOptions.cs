#nullable enable 

namespace Windows.ApplicationModel.Contacts
{
	public partial class ContactQueryOptions
	{
		public ContactQueryDesiredFields DesiredFields { get; set; }

		public ContactQueryTextSearch TextSearch { get; internal set; }

		public ContactQueryOptions(string text) 
		{
			TextSearch = new ContactQueryTextSearch(text, ContactQuerySearchFields.All);
		}

		public ContactQueryOptions(string text, ContactQuerySearchFields fields)
		{
			TextSearch = new ContactQueryTextSearch(text, fields);
		}

		public ContactQueryOptions()
		{
			TextSearch = new ContactQueryTextSearch("", ContactQuerySearchFields.None);
		}

	}
}


