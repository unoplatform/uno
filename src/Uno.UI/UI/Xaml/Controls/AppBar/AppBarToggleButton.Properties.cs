using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Controls
{
	partial class AppBarToggleButton
	{
		#region TemplateSettings
		public AppBarToggleButtonTemplateSettings TemplateSettings
		{
			get { return (AppBarToggleButtonTemplateSettings)this.GetValue(TemplateSettingsProperty); }
			set { this.SetValue(TemplateSettingsProperty, value); }
		}
		public static DependencyProperty TemplateSettingsProperty { get; } =
			DependencyProperty.Register(nameof(TemplateSettings), typeof(AppBarToggleButtonTemplateSettings), typeof(AppBarToggleButton), new FrameworkPropertyMetadata(null));
		#endregion

		#region Label

		public string Label
		{
			get => (string)GetValue(LabelProperty) ?? string.Empty;
			set => SetValue(LabelProperty, value);
		}

		public static DependencyProperty LabelProperty { get; } =
			DependencyProperty.Register(
				"Label", typeof(string),
				typeof(AppBarToggleButton),
				new FrameworkPropertyMetadata(default(string))
			);

		#endregion

		#region Icon

		public IconElement Icon
		{
			get => (IconElement)GetValue(IconProperty);
			set => SetValue(IconProperty, value);
		}

		public static DependencyProperty IconProperty { get; } =
			DependencyProperty.Register(
				"Icon",
				typeof(IconElement),
				typeof(AppBarToggleButton),
				new FrameworkPropertyMetadata(default(IconElement))
			);

		#endregion

		#region IsInOverflow

		public bool IsInOverflow
		{
			get => CommandBar.IsCommandBarElementInOverflow(this);
			internal set => this.SetValue(IsInOverflowProperty, value);
		}

		bool ICommandBarElement3.IsInOverflow
		{
			get => IsInOverflow;
			set => IsInOverflow = value;
		}

		public static DependencyProperty IsInOverflowProperty { get; } =
			DependencyProperty.Register(
				"IsInOverflow",
				typeof(bool),
				typeof(AppBarToggleButton),
				new FrameworkPropertyMetadata(false));

		#endregion

		#region LabelPosition

		public CommandBarLabelPosition LabelPosition
		{
			get => (CommandBarLabelPosition)this.GetValue(LabelPositionProperty);
			set => this.SetValue(LabelPositionProperty, value);
		}

		public static DependencyProperty LabelPositionProperty { get; } =
			DependencyProperty.Register(
				"LabelPosition",
				typeof(CommandBarLabelPosition),
				typeof(AppBarToggleButton),
				new FrameworkPropertyMetadata(default(CommandBarLabelPosition))
			);

		#endregion

		#region IsCompat

		public bool IsCompact
		{
			get => (bool)this.GetValue(IsCompactProperty);
			set => this.SetValue(IsCompactProperty, value);
		}

		public static DependencyProperty IsCompactProperty { get; } =
			DependencyProperty.Register(
				"IsCompact",
				typeof(bool),
				typeof(AppBarToggleButton),
				new FrameworkPropertyMetadata(default(bool))
			);

		#endregion

		#region DynamicOverflowOrder

		public int DynamicOverflowOrder
		{
			get => (int)this.GetValue(DynamicOverflowOrderProperty);
			set => this.SetValue(DynamicOverflowOrderProperty, value);
		}

		public static DependencyProperty DynamicOverflowOrderProperty { get; } =
			DependencyProperty.Register(
				"DynamicOverflowOrder",
				typeof(int),
				typeof(AppBarToggleButton),
				new FrameworkPropertyMetadata(default(int))
			);

		#endregion

		#region UseOverflowStyle

		internal bool UseOverflowStyle
		{
			get => (bool)this.GetValue(UseOverflowStyleProperty);
			set => this.SetValue(UseOverflowStyleProperty, value);
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
				typeof(AppBarToggleButton),
				new FrameworkPropertyMetadata(default(bool))
			);

		#endregion

		#region KeyboardAcceleratorTextOverride

		public string KeyboardAcceleratorTextOverride
		{
			get => GetKeyboardAcceleratorText();
			set => PutKeyboardAcceleratorText(value);
		}

		public static DependencyProperty KeyboardAcceleratorTextOverrideProperty { get; } =
			DependencyProperty.Register(
				nameof(KeyboardAcceleratorTextOverride),
				typeof(string),
				typeof(AppBarToggleButton),
				new FrameworkPropertyMetadata(default(string))
			);

		#endregion
	}
}
