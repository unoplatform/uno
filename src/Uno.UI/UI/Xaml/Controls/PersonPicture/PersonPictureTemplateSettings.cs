// MUX Reference PersonPictureTemplateSettings.properties.cpp, commit de78834

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	public partial class PersonPictureTemplateSettings : DependencyObject
	{
		public ImageBrush ActualImageBrush
		{
			get { return (ImageBrush)GetValue(ActualImageBrushProperty); }
			internal set { SetValue(ActualImageBrushProperty, value); }
		}

		public static DependencyProperty ActualImageBrushProperty { get ; } =
			DependencyProperty.Register(nameof(ActualImageBrush), typeof(ImageBrush), typeof(PersonPictureTemplateSettings), new FrameworkPropertyMetadata(null));

		public string ActualInitials
		{
			get { return (string)GetValue(ActualInitialsProperty); }
			internal set { SetValue(ActualInitialsProperty, value); }
		}

		public static DependencyProperty ActualInitialsProperty { get ; } =
			DependencyProperty.Register(nameof(ActualInitials), typeof(string), typeof(PersonPictureTemplateSettings), new FrameworkPropertyMetadata(""));
	}
}
