using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Windows.ApplicationModel.Contacts
{

	public partial class ContactBatch
	{

		public IReadOnlyList<Contact> Contacts { get; }

		internal ContactBatch(IReadOnlyList<Contact> contacts)
		{
			Contacts = contacts;
		}


	}
}
