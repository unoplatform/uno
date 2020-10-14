namespace Windows.ApplicationModel.Contacts
{
	public partial class ContactAddress
	{
		public ContactAddress()
		{
		}

		public string StreetAddress { get; set; }

		public string Region { get; set; }

		public string PostalCode { get; set; }

		public string Locality { get; set; }

		public ContactAddressKind Kind { get; set; }

		public string Description { get; set; }

		public  string Country { get; set; }		
	}
}
