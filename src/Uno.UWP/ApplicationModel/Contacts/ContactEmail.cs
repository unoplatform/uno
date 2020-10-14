using Uno.Logging;
using Microsoft.Extensions.Logging;
using Uno.Extensions;

namespace Windows.ApplicationModel.Contacts
{
	public partial class ContactEmail
	{
		public ContactEmailKind Kind { get; set; }

		public string Description { get; set; }

		public string Address { get; set; }
	}
}
