#nullable enable

using Uno.Logging;
using Microsoft.Extensions.Logging;
using Uno.Extensions;

namespace Windows.ApplicationModel.Contacts
{


	/// <summary>
	/// Represents the address of a contact.
	/// </summary>
	public partial class ContactAddress
	{

		/// <summary>
		/// Gets and sets the kind of contact address.
		/// </summary>
		public ContactAddressKind Kind { get; set; } = ContactAddressKind.Home;

		private string _streetAddress = string.Empty;
		/// <summary>
		/// Gets and sets the street address of a contact address.
		/// </summary>
		public string StreetAddress
		{
			get => _streetAddress;
			set
			{
				_streetAddress = value;
				LogWarningIfLenExceedsUWPLimit(value, 1024, "StreetAddress");
			}
		}

		private string _region = string.Empty;
		/// <summary>
		/// Gets and sets the region of a contact address.
		/// </summary>
		public string Region
		{
			get => _region;
			set
			{
				_region = value;
				LogWarningIfLenExceedsUWPLimit(value, 1024, "Region");
			}
		}

		private string _postalCode = string.Empty;
		/// <summary>
		/// Gets and sets the postal code of a contact address.
		/// </summary>
		public string PostalCode
		{
			get => _postalCode;
			set
			{
				_postalCode = value;
				LogWarningIfLenExceedsUWPLimit(value, 1024, "PostalCode");
			}
		}

		private string _locality = string.Empty;
		/// <summary>
		/// Gets and sets the locality of a contact address.
		/// </summary>
		public string Locality
		{
			get => _locality;
			set
			{
				_locality = value;
				LogWarningIfLenExceedsUWPLimit(value, 1024, "Locality");
			}
		}

		private string _country = string.Empty;
		/// <summary>
		/// Gets and sets the country of a contact address.
		/// </summary>
		public string Country
		{
			get => _country;
			set
			{
				_country = value;
				LogWarningIfLenExceedsUWPLimit(value, 1024, "Country");
			}
		}

		private string _description = string.Empty;

		/// <summary>
		/// Gets and sets the description of a contact address.
		/// </summary>
		public string Description
		{
			get => _description;
			set
			{
				_description = value;
				LogWarningIfLenExceedsUWPLimit(value, 512, "Country");
			}
		}

		private void LogWarningIfLenExceedsUWPLimit(string value, int lenLimit, string variableName)
		{
			if (value.Length > lenLimit)
			{
				if (!this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().LogWarning($"Windows.ApplicationModel.Contacts.ContactAddress.{variableName} is set to string longer than UWP limit ({lenLimit} chars)");
				}
			}
		}


	}
}
