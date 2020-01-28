#if __ANDROID__

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.ApplicationModel.Contacts
{
	public partial class ContactStore
	{
		public ContactReader GetContactReader() => GetContactReader(new ContactQueryOptions("", ContactQuerySearchFields.None));

		public ContactReader GetContactReader(ContactQueryOptions options) => new ContactReader(options);
	}
}

#endif 
