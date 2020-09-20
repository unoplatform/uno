using Uno.Logging;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using System.Configuration;

namespace Windows.ApplicationModel.Contacts
{
	public partial class ContactEmail
	{
		private string _Address;

		public ContactEmailKind Kind { get; set; }
		public string Address
		{
			get => this._Address;
			set
			{
				this._Address = value;
				if (this._Address.Length > 321)
				{
					this.Log().Warn("Windows.ApplicationModel.Contacts.ContactEmail.Address is set to string longer than UWP limit (321 chars)");
				}
			}
		}
	}
}
