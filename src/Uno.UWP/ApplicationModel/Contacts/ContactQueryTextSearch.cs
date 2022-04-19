#nullable enable

namespace Windows.ApplicationModel.Contacts
{
	public partial class ContactQueryTextSearch
	{
		public string Text { get; set; }
		public  ContactQuerySearchFields Fields { get; set; }
		internal ContactQueryTextSearch(string text, ContactQuerySearchFields fields)
		{
			Text = text;
			Fields = fields;
		}
	}
}
