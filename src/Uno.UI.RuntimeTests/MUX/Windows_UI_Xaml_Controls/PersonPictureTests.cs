// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Reflection;
using System.Threading;
using Windows.ApplicationModel.Contacts;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using MUXControlsTestApp.Utilities;
using Common;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;

#if USING_TAEF
using WEX.TestExecution;
using WEX.TestExecution.Markup;
using WEX.Logging.Interop;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
#endif

using PersonPicture = Windows.UI.Xaml.Controls.PersonPicture;

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests
{
	[TestClass]
	public class PersonPictureTests
	{
		[TestMethod]
		public async Task VerifyDefaultsAndBasicSetting()
		{
			await RunOnUIThread.Execute(() =>
			{
				PersonPicture personPicture = new PersonPicture();
				Verify.IsNotNull(personPicture);

				// Confirm initial dependency property values
				Verify.AreEqual(personPicture.BadgeGlyph, "");
				Verify.AreEqual(personPicture.BadgeNumber, 0);
				Verify.AreEqual(personPicture.IsGroup, false);
				Verify.AreEqual(personPicture.PreferSmallImage, false);
				Verify.AreEqual(personPicture.ProfilePicture, null);
				Verify.AreEqual(personPicture.Contact, null);
				Verify.AreEqual(personPicture.DisplayName, "");
				Verify.AreEqual(personPicture.Initials, "");

				// Now verify setting/retrieving the parameters
				personPicture.BadgeGlyph = "\uE765";
				Verify.AreEqual(personPicture.BadgeGlyph, "\uE765");
				personPicture.BadgeNumber = 10;
				Verify.AreEqual(personPicture.BadgeNumber, 10);
				personPicture.IsGroup = true;
				Verify.AreEqual(personPicture.IsGroup, true);
				personPicture.PreferSmallImage = true;
				Verify.AreEqual(personPicture.PreferSmallImage, true);
				personPicture.DisplayName = "Some Name";
				Verify.AreEqual(personPicture.DisplayName, "Some Name");
				personPicture.Initials = "MS";
				Verify.AreEqual(personPicture.Initials, "MS");

				if (ApiInformation.IsTypePresent("Windows.ApplicationModel.Contacts.Contact"))
				{
					Contact contact = new Contact();
					contact.FirstName = "FirstName";
					personPicture.Contact = contact;
					Verify.AreEqual(personPicture.Contact.FirstName, "FirstName");
				}

				ImageSource imageSrc = new BitmapImage(new Uri("ms-appx:/Assets/StoreLogo.png"));
				personPicture.ProfilePicture = imageSrc;
				Verify.IsNotNull(personPicture.ProfilePicture);
			});
		}

		[TestMethod]
		public async Task VerifyAutomationName()
		{
			await RunOnUIThread.Execute(() =>
			{
				PersonPicture personPicture = new PersonPicture();
				Verify.IsNotNull(personPicture);

				// Set properties and ensure that the AutomationName updates accordingly
				personPicture.Initials = "AB";
				String automationName = AutomationProperties.GetName(personPicture);
				Verify.AreEqual(automationName, "AB");

				personPicture.DisplayName = "Jane Smith";
				automationName = AutomationProperties.GetName(personPicture);
				Verify.AreEqual(automationName, "Jane Smith");

				if (ApiInformation.IsTypePresent("Windows.ApplicationModel.Contacts.Contact"))
				{
					Contact contact = new Contact();
					contact.FirstName = "John";
					contact.LastName = "Doe";
					personPicture.Contact = contact;
					automationName = AutomationProperties.GetName(personPicture);
					Verify.AreEqual(automationName, "John Doe");

					personPicture.IsGroup = true;
					automationName = AutomationProperties.GetName(personPicture);
					Verify.AreEqual(automationName, "Group");
					personPicture.IsGroup = false;

					personPicture.BadgeGlyph = "\uE765";
					automationName = AutomationProperties.GetName(personPicture);
					Verify.AreEqual(automationName, "John Doe, icon");

					personPicture.BadgeText = "Skype";
					automationName = AutomationProperties.GetName(personPicture);
					Verify.AreEqual(automationName, "John Doe, Skype");
					personPicture.BadgeText = "";

					personPicture.BadgeNumber = 5;
					automationName = AutomationProperties.GetName(personPicture);
					Verify.AreEqual(automationName, "John Doe, 5 items");

					personPicture.BadgeText = "direct reports";
					automationName = AutomationProperties.GetName(personPicture);
					Verify.AreEqual(automationName, "John Doe, 5 direct reports");
				}
			});
		}

#if false // XamlControlsXamlMetaDataProvider is not supported
		// XamlControlsXamlMetaDataProvider does not exist in the OS repo,
		// so we can't execute this test as authored there.
		[TestMethod]
		public async Task VerifyContactPropertyMetadata()
		{
			await RunOnUIThread.Execute(() =>
			{
				Microsoft.UI.Xaml.XamlTypeInfo.XamlControlsXamlMetaDataProvider provider = new Microsoft.UI.Xaml.XamlTypeInfo.XamlControlsXamlMetaDataProvider();
				var picturePersonType = provider.GetXamlType(typeof(PersonPicture).FullName);
				var contactMember = picturePersonType.GetMember("Contact");
				var memberType = contactMember.Type;
				Verify.AreEqual(memberType.BaseType.FullName, "Object");
			});
		}
#endif

		[TestMethod]
		public async Task VerifySmallWidthAndHeightDoNotCrash()
		{
			PersonPicture personPicture = null;

			await RunOnUIThread.Execute(() =>
			{
				personPicture = new PersonPicture();
				Private.Infrastructure.TestServices.WindowHelper.WindowContent = personPicture;
			});

			await Private.Infrastructure.TestServices.WindowHelper.WaitForIdle();

			var sizeChangedEvent = new TaskCompletionSource<bool>();

			await RunOnUIThread.Execute(() =>
			{
				personPicture.SizeChanged += (sender, args) => sizeChangedEvent.TrySetResult(true);
				personPicture.Width = 0.4;
				personPicture.Height = 0.4;
			});

			await sizeChangedEvent.Task;
			await Private.Infrastructure.TestServices.WindowHelper.WaitForIdle();
		}

		[TestMethod]
		public async Task VerifyVSMStatesForPhotosAndInitials()
		{
			PersonPicture personPicture = null;
			TextBlock initialsTextBlock = null;

			await RunOnUIThread.Execute(() =>
			{
				personPicture = new PersonPicture();
				Private.Infrastructure.TestServices.WindowHelper.WindowContent = personPicture;
			});

			await Private.Infrastructure.TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread.Execute(() =>
			{
				initialsTextBlock = (TextBlock)VisualTreeUtils.FindVisualChildByName(personPicture, "InitialsTextBlock");
				personPicture.IsGroup = true;
			});

			await Private.Infrastructure.TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread.Execute(() =>
			{
				Verify.AreEqual(initialsTextBlock.FontFamily.Source, "Segoe MDL2 Assets");
				Verify.AreEqual(initialsTextBlock.Text, "\xE716");

				personPicture.IsGroup = false;
				personPicture.Initials = "JS";
			});

			await Private.Infrastructure.TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread.Execute(() =>
			{
#if false
				Verify.AreEqual(initialsTextBlock.FontFamily.Source, "Segoe UI");
#endif
				Verify.AreEqual(initialsTextBlock.Text, "JS");

				personPicture.Initials = "";
			});

			await Private.Infrastructure.TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread.Execute(() =>
			{
				Verify.AreEqual(initialsTextBlock.FontFamily.Source, "Segoe MDL2 Assets");
				Verify.AreEqual(initialsTextBlock.Text, "\xE77B");

				// Make sure that custom FontFamily takes effect after the control is created
				// and also goes back to the MDL2 font after setting IsGroup = true.
				personPicture.FontFamily = new FontFamily("Segoe UI Emoji");
				personPicture.Initials = "👍";
			});

			await Private.Infrastructure.TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread.Execute(() =>
			{
				Verify.AreEqual(initialsTextBlock.FontFamily.Source, "Segoe UI Emoji");
				Verify.AreEqual(initialsTextBlock.Text, "👍");

				personPicture.IsGroup = true;
			});

			await Private.Infrastructure.TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread.Execute(() =>
			{
				Verify.AreEqual(initialsTextBlock.FontFamily.Source, "Segoe MDL2 Assets");
				Verify.AreEqual(initialsTextBlock.Text, "\xE716");
			});
		}
	}
}
