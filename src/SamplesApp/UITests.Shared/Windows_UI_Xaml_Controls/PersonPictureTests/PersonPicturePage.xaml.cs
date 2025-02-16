// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.ObjectModel;
using MUXControlsTestApp.Utilities;
using Uno.UI.Samples.Controls;
using Windows.ApplicationModel.Contacts;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;

namespace MUXControlsTestApp
{
	[Sample("MUX", "PersonPicture")]
#pragma warning disable UXAML0002 // does not explicitly define the Windows.UI.Xaml.Controls.UserControl base type in code behind.
	public sealed partial class PersonPicturePage
#pragma warning restore UXAML0002 // does not explicitly define the Windows.UI.Xaml.Controls.UserControl base type in code behind.
	{
		private Uri imageUri = new Uri("ms-appx:///Assets/ingredient2.png");
		private bool primaryEllipseLoaded = false;

		public PersonPicturePage()
		{
			this.InitializeComponent();
			this.TestPersonPicture.Loaded += PersonPicture_Loaded;
			this.TestPersonPicture.LayoutUpdated += PersonPicture_LayoutUpdated;
		}

		private void PersonPicture_LayoutUpdated(object sender, object e)
		{
			// Register items that are delay loaded
			if (!primaryEllipseLoaded)
			{
				string primaryEllipseName = "PersonPictureEllipse";
				Ellipse primaryEllipse = VisualTreeUtils.FindVisualChildByName(this.TestPersonPicture, primaryEllipseName) as Ellipse;
				if (primaryEllipse != null)
				{
					// Capture initial state of the property
					PrimaryEllipseFillChanged(primaryEllipse, Ellipse.FillProperty);

					primaryEllipse.RegisterPropertyChangedCallback(Ellipse.FillProperty, new DependencyPropertyChangedCallback(PrimaryEllipseFillChanged));
					primaryEllipseLoaded = true;
				}
			}
		}

		private void PersonPicture_Loaded(object sender, RoutedEventArgs e)
		{
			string InitialTextBlockName = "InitialsTextBlock";
			TextBlock initialTextBlock = VisualTreeUtils.FindVisualChildByName(this.TestPersonPicture, InitialTextBlockName) as TextBlock;
			if (initialTextBlock != null)
			{
				AutomationProperties.SetName(initialTextBlock, InitialTextBlockName);
				AutomationProperties.SetAccessibilityView(initialTextBlock, AccessibilityView.Content);
			}

			string badgeTextBlockName = "BadgeNumberTextBlock";
			TextBlock badgeTextBlock = VisualTreeUtils.FindVisualChildByName(this.TestPersonPicture, badgeTextBlockName) as TextBlock;
			if (badgeTextBlock != null)
			{
				AutomationProperties.SetName(badgeTextBlock, badgeTextBlockName);
				AutomationProperties.SetAccessibilityView(badgeTextBlock, AccessibilityView.Content);
			}

			string badgeGlyphIconName = "BadgeGlyphIcon";
			FontIcon badgeFontIcon = VisualTreeUtils.FindVisualChildByName(this.TestPersonPicture, badgeGlyphIconName) as FontIcon;
			if (badgeFontIcon != null)
			{
				AutomationProperties.SetName(badgeFontIcon, badgeGlyphIconName);
				AutomationProperties.SetAccessibilityView(badgeFontIcon, AccessibilityView.Content);
			}

			string badgeEllipseName = "BadgingEllipse";
			Ellipse badgeEllipse = VisualTreeUtils.FindVisualChildByName(this.TestPersonPicture, badgeEllipseName) as Ellipse;
			if (badgeEllipse != null)
			{
				AutomationProperties.SetName(badgeEllipse, badgeEllipseName);
				AutomationProperties.SetAccessibilityView(badgeEllipse, AccessibilityView.Content);
			}

			// Uno docs: This currently fails and returns null.
			// https://github.com/unoplatform/uno/issues/7258
			//CollectionViewSource cvs = rootGrid.FindName("cvs") as CollectionViewSource;
			cvs.Source = GetGroupedPeople();
			cvs.IsSourceGrouped = true;
		}

		private void FindAndGiveAutomationNameToVisualChild(string childName)
		{
			DependencyObject obj = VisualTreeUtils.FindVisualChildByName(this.TestPersonPicture, childName);

			if (obj != null)
			{
				AutomationProperties.SetName(obj, childName);
			}
		}

		private void PrimaryEllipseFillChanged(DependencyObject o, DependencyProperty p)
		{
			if (((Ellipse)o).Fill != null)
			{
				BgEllipseFilled.IsChecked = true;
			}
			else
			{
				BgEllipseFilled.IsChecked = false;
			}
		}

		private void BadgeNumberTextBox_TextChanged(object sender, RoutedEventArgs e)
		{
			int result = 0;
			int.TryParse(BadgeNumberTextBox.Text, out result);
			TestPersonPicture.BadgeNumber = result;
		}

		private void BadgeGlyphTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			TestPersonPicture.BadgeGlyph = BadgeGlyphTextBox.Text;
		}

		private void InitialTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			TestPersonPicture.Initials = InitialTextBox.Text;
		}

		private void GroupCheckBox_Checked(object sender, RoutedEventArgs e)
		{
			TestPersonPicture.IsGroup = true;
		}

		private void GroupCheckBox_Unchecked(object sender, RoutedEventArgs e)
		{
			TestPersonPicture.IsGroup = false;
		}

		private void DisplayNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			TestPersonPicture.DisplayName = DisplayNameTextBox.Text;
		}

		private void ContactBtn_Click(object sender, RoutedEventArgs e)
		{
			Contact contact = new Contact();
			contact.FirstName = "Test";
			contact.LastName = "Contact";

			TestPersonPicture.Contact = contact;
		}

		private async void ContactImageBtn_Click(object sender, RoutedEventArgs e)
		{
			Contact contact = new Contact();
			contact.SourceDisplayPicture = await StorageFile.GetFileFromApplicationUriAsync(imageUri);

			TestPersonPicture.Contact = contact;
		}

		private void ClearContactBtn_Click(object sender, RoutedEventArgs e)
		{
			TestPersonPicture.Contact = null;
		}

		private void ImageBtn_Click(object sender, RoutedEventArgs e)
		{
			TestPersonPicture.ProfilePicture = new BitmapImage(imageUri);
		}

		private void ClearImageBtn_Click(object sender, RoutedEventArgs e)
		{
			TestPersonPicture.ProfilePicture = null;
		}

		private void TestPersonPicture_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (Math.Round(TestPersonPicture.Width) == Math.Round(TestPersonPicture.Height))
			{
				DimensionsMatch.IsChecked = true;
			}
			else
			{
				DimensionsMatch.IsChecked = false;
			}
		}

		private static ObservableCollection<SportsGroup> GetGroupedPeople()
		{
			ObservableCollection<SportsGroup> groupedPeople = new ObservableCollection<SportsGroup>();

			for (int i = 0; i < 4; ++i)
			{
				SportsGroup group = null;

				switch (i)
				{
					case 0:
						group = new SportsGroup("Tennis");

						group.Add(new SportsPerson(1, "RF", "Roger Federer"));
						group.Add(new SportsPerson(2, "RN", "Rafael Nadal"));
						group.Add(new SportsPerson(3, "ND", "Novak Djokovic"));
						group.Add(new SportsPerson(4, "AM", "Andy Murray"));
						group.Add(new SportsPerson(5, "GD", "Grigor Dimitrov"));
						break;

					case 1:
						group = new SportsGroup("Soccer");

						group.Add(new SportsPerson(6, "CR", "Cristiano Ronaldo"));
						group.Add(new SportsPerson(7, "LM", "Lionel Messi"));
						group.Add(new SportsPerson(8, "N", "Neymar"));
						group.Add(new SportsPerson(9, "AI", "Andres Iniesta"));
						group.Add(new SportsPerson(10, "GB", "Gareth Bale"));
						group.Add(new SportsPerson(11, "X", "Xavi"));
						group.Add(new SportsPerson(12, "JR", "James Rodriguez"));
						group.Add(new SportsPerson(13, "R", "Ronaldinho"));
						group.Add(new SportsPerson(14, "AR", "Arjen Robben"));
						break;

					case 2:
						group = new SportsGroup("Basketball");

						group.Add(new SportsPerson(15, "AI", "Allen Iverson"));
						group.Add(new SportsPerson(16, "DW", "Dwayne Wade"));
						group.Add(new SportsPerson(17, "LJ", "LeBron James"));
						group.Add(new SportsPerson(18, "KD", "Kevin Durant"));
						group.Add(new SportsPerson(19, "KB", "Kobe Bryant"));
						break;

					case 3:
						group = new SportsGroup("Formula 1");

						group.Add(new SportsPerson(20, "AP", "Alain Prost"));
						group.Add(new SportsPerson(21, "AS", "Ayrton Senna"));
						group.Add(new SportsPerson(22, "MS", "Michael Schumacher"));
						group.Add(new SportsPerson(23, "NL", "Niki Lauda"));
						group.Add(new SportsPerson(24, "SV", "Sebastian Vettel"));
						break;
				}

				groupedPeople.Add(group);
			}

			return groupedPeople;
		}

		public class SportsPerson
		{
			public SportsPerson(int badgeNumber, string initials, string displayName)
			{
				BadgeNumber = badgeNumber;
				Initials = initials;
				DisplayName = displayName;
			}

			public int BadgeNumber { get; set; }
			public string Initials { get; set; }
			public string DisplayName { get; set; }
		}

		public class SportsGroup : ObservableCollection<SportsPerson>
		{
			public SportsGroup(string name)
			{
				Name = name;
			}

			public string Name { get; set; }
		}
	}
}
