using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.ApplicationModel.Email
{
	public partial class EmailRecipient
	{
		private string _name = string.Empty;
		private string _address = string.Empty;

		public EmailRecipient()
		{
		}

		public EmailRecipient(string address)
		{
			Address = address ?? throw new ArgumentNullException(nameof(address));
		}

		public EmailRecipient(string address, string name) : this(address)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
		}

		public string Name
		{
			get => _name;
			set
			{
				_name = value ?? throw new ArgumentNullException(nameof(value));
			}
		}

		public string Address
		{
			get => _address;
			set
			{
				_address = value ?? throw new ArgumentNullException(nameof(value));
			}
		}
	}
}
