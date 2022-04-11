using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Data.Xml.Dom;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using uwpContacts = Windows.ApplicationModel.Contacts;

namespace UITests.Windows_ApplicationModel.Contacts
{
	[SampleControlInfo("Windows_ApplicationModel.Contacts", "ContactReader")]

	public sealed partial class ContactReader : Page
    {

		public ContactReader()
		{
			this.InitializeComponent();
		}

		private async void getContacts_Click(object sender, RoutedEventArgs e)
		{
			uiErrorMsg.Text = "";
			uiFullName.Text = "";
			uiPhone.Text = "";
			uiEmail.Text = "";

			Windows.ApplicationModel.Contacts.ContactStore oStore;
			try
			{
				oStore = await uwpContacts.ContactManager.RequestStoreAsync();  // as read only
			}
			catch(Exception ex)
			{
				uiErrorMsg.Text = "Exception while RequestStoreAsync: " + ex.Message;
				return;
			}
			if (oStore == null)
			{
				uiErrorMsg.Text = "Got null as store from RequestStoreAsync";
				return;
			}

			var oContactRdr = oStore.GetContactReader();
			if (oContactRdr == null)
			{
				uiErrorMsg.Text = "Got null as reader from GetContactReader";
				return;
			}

			var oBatch = await oContactRdr.ReadBatchAsync();
			if (oContactRdr == null)
			{
				uiErrorMsg.Text = "Got null as batch from ReadBatchAsync";
				return;
			}

			if (oBatch.Contacts.Count < 1)
			{
				uiErrorMsg.Text = "Seems like you have no contacts defined";
				return;
			}

			uiOkMsg.Text = "First batch contains " + oBatch.Contacts.Count.ToString() + " contacts, data from first:";

			var contact = oBatch.Contacts[0];

			uiFullName.Text = "Full name: " + contact.FullName;

			if(contact.Phones.Count < 1)
			{
				uiPhone.Text = "(no phone number)";
			}
			else
			{
				uiPhone.Text = contact.Phones[0].Number;

			}

			if (contact.Emails.Count < 1)
			{
				uiEmail.Text = "(no email)";
			}
			else
			{
				uiEmail.Text = contact.Emails[0].Address.ToString();

			}
		}

	}
}
