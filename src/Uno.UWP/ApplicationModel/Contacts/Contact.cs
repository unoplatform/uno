using System.Collections.Generic;
using Uno.Logging;
using Microsoft.Extensions.Logging;
using Uno.Extensions;

namespace Windows.ApplicationModel.Contacts
{
	public partial class Contact
	{
		public Contact()
		{
			Emails = new List<ContactEmail>();
			Phones = new List<ContactPhone>();
			Addresses = new List<ContactAddress>();
		}

		public IList<ContactEmail> Emails { get; internal set; }

		public IList<ContactAddress> Addresses { get; internal set; }

		public IList<ContactPhone> Phones { get; internal set; }

		public string FirstName { get; set; }

		public string MiddleName { get; set; }

		public string LastName { get; set; }

		public string HonorificNamePrefix { get; set; }

		public string HonorificNameSuffix { get; set; }
	}
}
