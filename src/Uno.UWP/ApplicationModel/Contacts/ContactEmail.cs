using Uno.Logging;
using Microsoft.Extensions.Logging;
using Uno.Extensions;

namespace Windows.ApplicationModel.Contacts
{
	public partial class ContactEmail
	{
		private string _address;

		public ContactEmailKind Kind { get; set; }

		public string Address
		{
			get => _address;
			set
			{
				_address = value;
				if (_address.Length > 321)
				{
					if (this.Log().IsEnabled(LogLevel.Warning))
					{
						this.Log().LogWarning("Windows.ApplicationModel.Contacts.ContactEmail.Address is set to string longer than UWP limit (321 chars)");
					}
				}
			}
		}
	}
}
