#nullable enable

using System.Collections.Generic;
using System.Linq;
using Uno;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using Uno.Foundation.Logging;

using Uno.Extensions;

namespace Windows.ApplicationModel.Contacts
{
	/// <summary>
	/// Represents a contact.
	/// </summary>
	public partial class Contact
	{
		[NotImplemented]
		public string? Id { get; set; }

		/// <summary>
		/// Gets the full name of the Contact.
		/// </summary>
		public string FullName
		{
			get
			{
				if (string.IsNullOrEmpty(DisplayNameOverride))
				{
					return string.Join(' ', new[] { HonorificNamePrefix, FirstName, MiddleName, LastName, HonorificNameSuffix }.Where(n => !string.IsNullOrEmpty(n)));
				}
				return DisplayNameOverride;
			}
		}

		/// <summary>
		/// Gets the display name for a contact.
		/// </summary>
		public string DisplayName
		{
			get
			{
				if (string.IsNullOrEmpty(DisplayNameOverride))
				{
					return string.Join(' ', new[] { HonorificNamePrefix, FirstName, LastName, HonorificNameSuffix }.Where(n => !string.IsNullOrEmpty(n)));
				}
				return DisplayNameOverride;
			}
		}

		/// <summary>
		/// Gets or sets the display that was manually entered by the user.
		/// </summary>
		public string DisplayNameOverride { get; set; } = "";

		/// <summary>
		/// Gets the email addresses for a contact.
		/// </summary>
		public IList<ContactEmail> Emails { get; internal set; } = new NonNullList<ContactEmail>();

		/// <summary>
		/// Gets the contact addresses for a contact.
		/// </summary>
		public IList<ContactAddress> Addresses { get; internal set; } = new NonNullList<ContactAddress>();

		/// <summary>
		/// Gets info about the phones for a contact.
		/// </summary>
		public IList<ContactPhone> Phones { get; internal set; } = new NonNullList<ContactPhone>();

		private string _firstName = "";
		/// <summary>
		/// Gets and sets the first name for a contact.
		/// </summary>
		public string FirstName
		{
			get => _firstName;
			set
			{
				_firstName = value;
				LogWarningIfLenExceedsUWPLimit(value, 64, "FirstName");
			}
		}

		private string _middleName = "";
		/// <summary>
		/// Gets and sets the middle name for a contact.
		/// </summary>
		public string MiddleName
		{
			get => _middleName;
			set
			{
				_middleName = value;
				LogWarningIfLenExceedsUWPLimit(value, 64, "MiddleName");
			}
		}

		private string _lastName = "";
		/// <summary>
		/// Gets and sets the last name for a contact.
		/// </summary>
		public string LastName
		{
			get => _lastName;
			set
			{
				_lastName = value;
				LogWarningIfLenExceedsUWPLimit(value, 64, "LastName");
			}
		}

		private string _honorificNamePrefix = "";
		/// <summary>
		/// Gets and sets the honorific prefix for the name for a contact.
		/// </summary>
		public string HonorificNamePrefix
		{
			get => _honorificNamePrefix;
			set
			{
				_honorificNamePrefix = value;
				LogWarningIfLenExceedsUWPLimit(value, 64, "HonorificNamePrefix");
			}
		}

		private string _honorificNameSuffix = "";
		/// <summary>
		/// Gets and sets the honorific suffix for the name for a contact.
		/// </summary>
		public string HonorificNameSuffix
		{
			get => _honorificNameSuffix;
			set
			{
				_honorificNameSuffix = value;
				LogWarningIfLenExceedsUWPLimit(value, 64, "HonorificNameSuffix");
			}
		}

		/// <summary>
		/// Gets or sets the nickname for the Contact.
		/// </summary>
		public string Nickname { get; set; } = "";

		private string _notes = "";
		/// <summary>
		/// Gets and sets notes for a contact.
		/// </summary>
		public string Notes
		{
			get => _notes;
			set
			{
				_notes = value;
				LogWarningIfLenExceedsUWPLimit(value, 4096, "Notes");
			}
		}

		private string _yomiGivenName = "";
		/// <summary>
		/// Gets the Yomi (phonetic Japanese equivalent) given name for a contact.
		/// </summary>
		public string YomiGivenName
		{
			get => _yomiGivenName;
			set
			{
				_yomiGivenName = value;
				LogWarningIfLenExceedsUWPLimit(value, 120, "YomiGivenName");
			}
		}

		private string _yomiFamilyName = "";
		/// <summary>
		/// Gets the Yomi (phonetic Japanese equivalent) family name for a contact.
		/// </summary>
		public string YomiFamilyName
		{
			get => _yomiFamilyName;
			set
			{
				_yomiFamilyName = value;
				LogWarningIfLenExceedsUWPLimit(value, 120, "YomiFamilyName");
			}
		}

		[NotImplemented]
		public IRandomAccessStreamReference? Thumbnail { get; set; }

		[NotImplemented]
		public IRandomAccessStreamReference? LargeDisplayPicture { get; }

		[NotImplemented]
		public IRandomAccessStreamReference? SmallDisplayPicture { get; }

		[NotImplemented]
		public IRandomAccessStreamReference? SourceDisplayPicture { get; set; }

		private void LogWarningIfLenExceedsUWPLimit(string value, int lenLimit, string variableName)
		{
			if (value.Length > lenLimit)
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().LogWarning($"Windows.ApplicationModel.Contacts.Contact.{variableName} is set to string longer than UWP limit ({lenLimit} chars)");
				}
			}
		}




	}
}
