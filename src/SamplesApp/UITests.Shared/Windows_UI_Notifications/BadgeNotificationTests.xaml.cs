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
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace UITests.Windows_UI_Notifications
{
	[Sample("Windows.UI.Notifications")]
	public sealed partial class BadgeNotificationTests : Page
	{
		public BadgeNotificationTests()
		{
			this.InitializeComponent();
		}

		private void SetBadge_Click(object sender, RoutedEventArgs e)
		{
			var badgeXml = BadgeUpdateManager.GetTemplateContent(BadgeTemplateType.BadgeNumber);
			var badgeGlyphXml = BadgeUpdateManager.GetTemplateContent(BadgeTemplateType.BadgeGlyph);
			XmlElement badgeElement = badgeXml.SelectSingleNode("/badge") as XmlElement;
			badgeElement.SetAttribute("value", BadgeTextBox.Text);

			var badgeNotification = new BadgeNotification(badgeXml);

			BadgeUpdateManager.CreateBadgeUpdaterForApplication().Update(badgeNotification);
		}

		private void ClearBadge_Click(object sender, RoutedEventArgs e)
		{
			BadgeUpdateManager.CreateBadgeUpdaterForApplication().Clear();
		}
	}
}
