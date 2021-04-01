#nullable enable

using System.Collections.Generic;
using System.Linq;
using Uno;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;

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
					return string.Join(" ", new [] { HonorificNamePrefix, FirstName, MiddleName, LastName, HonorificNameSuffix }.Where(n => !string.IsNullOrEmpty(n)));
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
					return string.Join(" ", new[] { HonorificNamePrefix, FirstName, LastName, HonorificNameSuffix }.Where(n => !string.IsNullOrEmpty(n)));
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

		/// <summary>
		/// Gets and sets the first name for a contact.
		/// </summary>
		public string FirstName { get; set; } = "";

		/// <summary>
		/// Gets and sets the middle name for a contact.
		/// </summary>
		public string MiddleName { get; set; } = "";

		/// <summary>
		/// Gets and sets the last name for a contact.
		/// </summary>
		public string LastName { get; set; } = "";

		/// <summary>
		/// Gets and sets the honorific prefix for the name for a contact.
		/// </summary>
		public string HonorificNamePrefix { get; set; } = "";

		/// <summary>
		/// Gets and sets the honorific suffix for the name for a contact.
		/// </summary>
		public string HonorificNameSuffix { get; set; } = "";

		/// <summary>
		/// Gets or sets the nickname for the Contact.
		/// </summary>
		public string Nickname { get; set; } = "";

		/// <summary>
		/// Gets and sets notes for a contact.
		/// </summary>
		public string Notes { get; set; } = "";

		/// <summary>
		/// Gets the Yomi (phonetic Japanese equivalent) given name for a contact.
		/// </summary>
		public string YomiGivenName { get; set; } = "";

		/// <summary>
		/// Gets the Yomi (phonetic Japanese equivalent) family name for a contact.
		/// </summary>
		public string YomiFamilyName { get; set; } = "";

		[NotImplemented]
		public IRandomAccessStreamReference? Thumbnail { get; set; }

		[NotImplemented]
		public IRandomAccessStreamReference? LargeDisplayPicture { get; }

		[NotImplemented]
		public IRandomAccessStreamReference? SmallDisplayPicture { get; }

		[NotImplemented]
		public IRandomAccessStreamReference? SourceDisplayPicture { get; set; }
	}
}
