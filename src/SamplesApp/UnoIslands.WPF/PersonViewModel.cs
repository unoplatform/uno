using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnoIslands.Skia.Wpf
{
	public class PersonViewModel
	{
		public string Name { get; set; }

		public string Phone { get; set; }

		public string Email { get; set; }

		public string Address { get; set; }

		public string PostalZip { get; set; }

		public string Region { get; set; }

		public string Country { get; set; }

		public string Note { get; set; }

		public string EmailUrl => "mailto:" + Email;
		
		public string ImageUrl => $"https://www.gravatar.com/avatar/{Name.GetHashCode()}?s=128&d=identicon&r=PG";
	}
}
