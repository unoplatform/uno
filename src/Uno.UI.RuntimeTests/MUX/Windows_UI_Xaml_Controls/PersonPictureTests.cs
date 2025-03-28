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

using PersonPicture = Microsoft/* UWP don't rename */.UI.Xaml.Controls.PersonPicture;

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests
{
	[TestClass]
	public class PersonPictureTests
	{
		[TestMethod]
		public async Task VerifyDefaultsAndBasicSetting()
		{
			await RunOnUIThread.ExecuteAsync(() =>
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
			if (ApiInformation.IsTypePresent("Windows.UI.Xaml.Automation.Peers.PersonPictureAutomationPeer"))
			{
				await RunOnUIThread.ExecuteAsync(() =>
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
		}

#if false // XamlControlsXamlMetaDataProvider is not supported
		// XamlControlsXamlMetaDataProvider does not exist in the OS repo,
		// so we can't execute this test as authored there.
		[TestMethod]
		public async Task VerifyContactPropertyMetadata()
		{
			await RunOnUIThread.ExecuteAsync(() =>
			{
				Windows.UI.Xaml.XamlTypeInfo.XamlControlsXamlMetaDataProvider provider = new Windows.UI.Xaml.XamlTypeInfo.XamlControlsXamlMetaDataProvider();
				var picturePersonType = provider.GetXamlType(typeof(PersonPicture).FullName);
				var contactMember = picturePersonType.GetMember("Contact");
				var memberType = contactMember.Type;
				Verify.AreEqual(memberType.BaseType.FullName, "Object");
			});
		}
#endif

		[TestMethod]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task VerifySmallWidthAndHeightDoNotCrash()
		{
			PersonPicture personPicture = null;

			await RunOnUIThread.ExecuteAsync(() =>
			{
				personPicture = new PersonPicture();
				global::Private.Infrastructure.TestServices.WindowHelper.WindowContent = personPicture;
			});

			await global::Private.Infrastructure.TestServices.WindowHelper.WaitForIdle();

			var sizeChangedEvent = new TaskCompletionSource<bool>();

			var cts = new CancellationTokenSource(1000);
			cts.Token.Register(() => sizeChangedEvent.TrySetException(new TimeoutException()));

			await RunOnUIThread.ExecuteAsync(() =>
			{
				personPicture.SizeChanged += (sender, args) => sizeChangedEvent.TrySetResult(true);
				personPicture.UpdateLayout();
				personPicture.Width = 0.4;
				personPicture.Height = 0.4;
				personPicture.UpdateLayout();
			});

			await sizeChangedEvent.Task;
			await global::Private.Infrastructure.TestServices.WindowHelper.WaitForIdle();
		}

#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		[TestMethod]
		public async Task VerifyVSMStatesForPhotosAndInitials()
		{
			PersonPicture personPicture = null;
			TextBlock initialsTextBlock = null;
#if WINAPPSDK
			string symbolsFontName = "Segoe MDL2 Assets";
#else
			string symbolsFontName = "ms-appx:///Uno.Fonts.Fluent/Fonts/uno-fluentui-assets.ttf";
#endif
			await RunOnUIThread.ExecuteAsync(() =>
			{
				personPicture = new PersonPicture();
				global::Private.Infrastructure.TestServices.WindowHelper.WindowContent = personPicture;
			});

			await global::Private.Infrastructure.TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread.ExecuteAsync(() =>
			{
				initialsTextBlock = (TextBlock)VisualTreeUtils.FindVisualChildByName(personPicture, "InitialsTextBlock");
				personPicture.IsGroup = true;
			});

			await global::Private.Infrastructure.TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread.ExecuteAsync(() =>
			{
				Verify.AreEqual(initialsTextBlock.FontFamily.Source, symbolsFontName);
				Verify.AreEqual(initialsTextBlock.Text, "\xE716");

				personPicture.IsGroup = false;
				personPicture.Initials = "JS";
			});

			await global::Private.Infrastructure.TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread.ExecuteAsync(() =>
			{
#if false
				Verify.AreEqual(initialsTextBlock.FontFamily.Source, "Segoe UI");
#endif
				Verify.AreEqual(initialsTextBlock.Text, "JS");

				personPicture.Initials = "";
			});

			await global::Private.Infrastructure.TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread.ExecuteAsync(() =>
			{
				Verify.AreEqual(initialsTextBlock.FontFamily.Source, symbolsFontName);
				Verify.AreEqual(initialsTextBlock.Text, "\xE77B");

				// Make sure that custom FontFamily takes effect after the control is created
				// and also goes back to the MDL2 font after setting IsGroup = true.
				personPicture.FontFamily = new FontFamily("Segoe UI Emoji");
				personPicture.Initials = "👍";
			});

			await global::Private.Infrastructure.TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread.ExecuteAsync(() =>
			{
				Verify.AreEqual(initialsTextBlock.FontFamily.Source, "Segoe UI Emoji");
				Verify.AreEqual(initialsTextBlock.Text, "👍");

				personPicture.IsGroup = true;
			});

			await global::Private.Infrastructure.TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread.ExecuteAsync(() =>
			{
				Verify.AreEqual(initialsTextBlock.FontFamily.Source, symbolsFontName);
				Verify.AreEqual(initialsTextBlock.Text, "\xE716");
			});
		}
	}
}
