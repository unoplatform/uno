// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference PipsPager.properties.cpp, commit 43a110c

using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class PipsPager
	{
		public int MaxVisiblePips
		{
			get => (int)GetValue(MaxVisiblePipsProperty);
			set => SetValue(MaxVisiblePipsProperty, value);
		}

		public static DependencyProperty MaxVisiblePipsProperty { get; } =
			DependencyProperty.Register(
				nameof(MaxVisiblePips),
				typeof(int),
				typeof(PipsPager),
				new FrameworkPropertyMetadata(5, OnPropertyChanged));

		public Style NextButtonStyle
		{
			get => (Style)GetValue(NextButtonStyleProperty);
			set => SetValue(NextButtonStyleProperty, value);
		}

		public static DependencyProperty NextButtonStyleProperty { get; } =
			DependencyProperty.Register(
				nameof(NextButtonStyle),
				typeof(Style),
				typeof(PipsPager),
				new FrameworkPropertyMetadata(null, OnPropertyChanged));




		public PipsPagerButtonVisibility NextButtonVisibility
		{
			get => (PipsPagerButtonVisibility)GetValue(NextButtonVisibilityProperty);
			set => SetValue(NextButtonVisibilityProperty, value);
		}

		public static DependencyProperty NextButtonVisibilityProperty { get; } =
			DependencyProperty.Register(
				nameof(NextButtonVisibility),
				typeof(PipsPagerButtonVisibility),
				typeof(PipsPager),
				new FrameworkPropertyMetadata(PipsPagerButtonVisibility.Collapsed));

		public Style NormalPipStyle
		{
			get => (Style)GetValue(NormalPipStyleProperty);
			set => SetValue(NormalPipStyleProperty, value);
		}

		public static DependencyProperty NormalPipStyleProperty { get; } =
			DependencyProperty.Register(
				nameof(NormalPipStyle),
				typeof(Style),
				typeof(PipsPager),
				new FrameworkPropertyMetadata(null, OnPropertyChanged));

		public int NumberOfPages
		{
			get => (int)GetValue(NumberOfPagesProperty);
			set => SetValue(NumberOfPagesProperty, value);
		}

		public static DependencyProperty NumberOfPagesProperty { get; } =
			DependencyProperty.Register(
				nameof(NumberOfPages),
				typeof(int),
				typeof(PipsPager),
				new FrameworkPropertyMetadata(-1, OnPropertyChanged));

		public Orientation Orientation
		{
			get => (Orientation)GetValue(OrientationProperty);
			set => SetValue(OrientationProperty, value);
		}

		public static DependencyProperty OrientationProperty { get; } =
			DependencyProperty.Register(
				nameof(Orientation),
				typeof(Orientation),
				typeof(PipsPager),
				new FrameworkPropertyMetadata(Orientation.Horizontal, OnPropertyChanged));

		public Style PreviousButtonStyle
		{
			get => (Style)GetValue(PreviousButtonStyleProperty);
			set => SetValue(PreviousButtonStyleProperty, value);
		}

		public static DependencyProperty PreviousButtonStyleProperty { get; } =
			DependencyProperty.Register(
				nameof(PreviousButtonStyle),
				typeof(Style),
				typeof(PipsPager),
				new FrameworkPropertyMetadata(null, OnPropertyChanged));



		public PipsPagerButtonVisibility PreviousButtonVisibility
		{
			get => (PipsPagerButtonVisibility)GetValue(PreviousButtonVisibilityProperty);
			set => SetValue(PreviousButtonVisibilityProperty, value);
		}

		public static DependencyProperty PreviousButtonVisibilityProperty { get; } =
			DependencyProperty.Register(
				nameof(PreviousButtonVisibility),
				typeof(PipsPagerButtonVisibility),
				typeof(PipsPager),
				new FrameworkPropertyMetadata(PipsPagerButtonVisibility.Collapsed, OnPropertyChanged));

		public int SelectedPageIndex
		{
			get => (int)GetValue(SelectedPageIndexProperty);
			set => SetValue(SelectedPageIndexProperty, value);
		}

		public static DependencyProperty SelectedPageIndexProperty { get; } =
			DependencyProperty.Register(
				nameof(SelectedPageIndex),
				typeof(int),
				typeof(PipsPager),
				new FrameworkPropertyMetadata(0, OnPropertyChanged));

		public Style SelectedPipStyle
		{
			get => (Style)GetValue(SelectedPipStyleProperty);
			set => SetValue(SelectedPipStyleProperty, value);
		}

		public static DependencyProperty SelectedPipStyleProperty { get; } =
			DependencyProperty.Register(
				nameof(SelectedPipStyle),
				typeof(Style),
				typeof(PipsPager),
				new FrameworkPropertyMetadata(null, OnPropertyChanged));

		public PipsPagerTemplateSettings TemplateSettings
		{
			get => (PipsPagerTemplateSettings)GetValue(TemplateSettingsProperty);
			set => SetValue(TemplateSettingsProperty, value);
		}

		public static DependencyProperty TemplateSettingsProperty { get; } =
			DependencyProperty.Register(
				nameof(TemplateSettings),
				typeof(PipsPagerTemplateSettings),
				typeof(PipsPager),
				new FrameworkPropertyMetadata(null, OnPropertyChanged));

		private static void OnPropertyChanged(
			DependencyObject sender,
			DependencyPropertyChangedEventArgs args)
		{
			var owner = (PipsPager)sender;
			owner.OnPropertyChanged(args);
		}

		// Events

		public event TypedEventHandler<PipsPager, PipsPagerSelectedIndexChangedEventArgs> SelectedIndexChanged;
	}
}
