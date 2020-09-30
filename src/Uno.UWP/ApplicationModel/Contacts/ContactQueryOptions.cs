
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Windows.ApplicationModel.Contacts
{
	public partial class ContactQueryOptions
	{
		internal ContactQuerySearchFields SearchFields { get; set; }
		internal string SearchText { get; set; }

		public ContactQueryDesiredFields DesiredFields { get; set; }

		public ContactQueryOptions(string text) 
		{
			SearchText = text;
			SearchFields = ContactQuerySearchFields.All;
		}

		public ContactQueryOptions(string text, ContactQuerySearchFields fields)
		{
			SearchText = text;
			SearchFields = fields;
		}

		public ContactQueryOptions()
		{
			SearchText = "";
			SearchFields = ContactQuerySearchFields.None;
		}

	}
}


