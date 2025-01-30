#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls
{
	partial class AppBarButton
	{
		#region TemplateSettings
		public AppBarButtonTemplateSettings TemplateSettings
		{
			get => (AppBarButtonTemplateSettings)GetValue(TemplateSettingsProperty);
			set => SetValue(TemplateSettingsProperty, value);
		}
		public static DependencyProperty TemplateSettingsProperty { get; } =
			DependencyProperty.Register(nameof(TemplateSettings), typeof(AppBarButtonTemplateSettings), typeof(AppBarButton), new FrameworkPropertyMetadata(null));
		#endregion

		/// <summary>
		/// Gets or sets a string that overrides the default key combination string associated with a keyboard accelerator.
		/// </summary>
		public string KeyboardAcceleratorTextOverride
		{
			get => AppBarButtonHelpers<AppBarButton>.GetKeyboardAcceleratorText(this);
			set => AppBarButtonHelpers<AppBarButton>.PutKeyboardAcceleratorText(this, value);
		}

		/// <summary>
		/// Identifies the AppBarButton.KeyboardAcceleratorTextOverride dependency property.
		/// </summary>
		public static DependencyProperty KeyboardAcceleratorTextOverrideProperty { get; } =
			DependencyProperty.Register(
				nameof(KeyboardAcceleratorTextOverride),
				typeof(string),
				typeof(AppBarButton),
				new FrameworkPropertyMetadata(default(string)));

		/// <summary>
		/// Gets or sets the text displayed on the app bar button.
		/// </summary>
		public string Label
		{
			get => (string)GetValue(LabelProperty);
			set => SetValue(LabelProperty, value);
		}

		/// <summary>
		/// Identifies the Label dependency property.
		/// </summary>
		public static DependencyProperty LabelProperty { get; } =
			DependencyProperty.Register(
				nameof(Label),
				typeof(string),
				typeof(AppBarButton),
				new FrameworkPropertyMetadata(default(string))
			);

		public IconElement Icon
		{
			get => (IconElement)GetValue(IconProperty);
			set => SetValue(IconProperty, value);
		}

		public static DependencyProperty IconProperty { get; } =
			DependencyProperty.Register(
				nameof(Icon),
				typeof(IconElement),
				typeof(AppBarButton),
				new FrameworkPropertyMetadata(default(IconElement)));

		public bool IsInOverflow
		{
			get => CommandBar.IsCommandBarElementInOverflow(this);
			internal set => SetValue(IsInOverflowProperty, value);
		}

		bool ICommandBarElement3.IsInOverflow
		{
			get => IsInOverflow;
			set => IsInOverflow = value;
		}

		public static DependencyProperty IsInOverflowProperty { get; } =
			DependencyProperty.Register(
				nameof(IsInOverflow),
				typeof(bool),
				typeof(AppBarButton),
				new FrameworkPropertyMetadata(false));

		public CommandBarLabelPosition LabelPosition
		{
			get => (CommandBarLabelPosition)GetValue(LabelPositionProperty);
			set => SetValue(LabelPositionProperty, value);
		}

		public static DependencyProperty LabelPositionProperty { get; } =
			DependencyProperty.Register(
				nameof(LabelPosition),
				typeof(CommandBarLabelPosition),
				typeof(AppBarButton),
				new FrameworkPropertyMetadata(default(CommandBarLabelPosition))
			);

		public bool IsCompact
		{
			get => (bool)GetValue(IsCompactProperty);
			set => SetValue(IsCompactProperty, value);
		}

		public static DependencyProperty IsCompactProperty { get; } =
			DependencyProperty.Register(
				nameof(IsCompact),
				typeof(bool),
				typeof(AppBarButton),
				new FrameworkPropertyMetadata(default(bool))
			);

		public int DynamicOverflowOrder
		{
			get => (int)GetValue(DynamicOverflowOrderProperty);
			set => SetValue(DynamicOverflowOrderProperty, value);
		}

		public static DependencyProperty DynamicOverflowOrderProperty { get; } =
			DependencyProperty.Register(
				nameof(DynamicOverflowOrder),
				typeof(int),
				typeof(AppBarButton),
				new FrameworkPropertyMetadata(default(int))
			);

		internal bool UseOverflowStyle
		{
			get => (bool)GetValue(UseOverflowStyleProperty);
			set => SetValue(UseOverflowStyleProperty, value);
		}

		bool ICommandBarOverflowElement.UseOverflowStyle
		{
			get => UseOverflowStyle;
			set => UseOverflowStyle = value;
		}

		internal static DependencyProperty UseOverflowStyleProperty { get; } =
			DependencyProperty.Register(
				nameof(UseOverflowStyle),
				typeof(bool),
				typeof(AppBarButton),
				new FrameworkPropertyMetadata(default(bool)));
	}
}
