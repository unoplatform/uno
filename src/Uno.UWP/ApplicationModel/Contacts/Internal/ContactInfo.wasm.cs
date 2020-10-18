using System;
using System.Runtime.Serialization;

namespace Uno.ApplicationModel.Contacts.Internal
{
	[DataContract]
	internal class ContactInfo
	{
		[DataMember(Name = "id")]
		public string Id { get; set; }

		[DataMember]
		public DateTime? LastUpdated { get; set; }
                attribute ContactName       name;
                attribute ContactField[]    emails;
                attribute DOMString[]       photos;
                attribute ContactField[]    urls;
                attribute DOMString[]       categories;
                attribute ContactAddress[]  addresses;
                attribute ContactTelField[] phoneNumbers;
                attribute DOMString[]       organizations;
                attribute DOMString[]       jobTitles;
                attribute Date?             birthday;
                attribute DOMString[]       notes;
                attribute ContactField[]    impp;
                attribute Date?             anniversary;
                attribute ContactGender     gender;
	}
}
