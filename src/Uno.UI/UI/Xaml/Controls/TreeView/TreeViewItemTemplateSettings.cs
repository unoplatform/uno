#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using System;

namespace Windows.UI.Xaml.Controls
{
	[Uno.NotImplemented]
	public partial class TreeViewItemTemplateSettings : DependencyObject
	{
		[Uno.NotImplemented]
		public Visibility CollapsedGlyphVisibility
		{
			get
			{
				return (Visibility)this.GetValue(CollapsedGlyphVisibilityProperty);
			}
		}

		[Uno.NotImplemented]
		public int DragItemsCount
		{
			get
			{
				return (int)this.GetValue(DragItemsCountProperty);
			}
		}

		[Uno.NotImplemented]
		public Visibility ExpandedGlyphVisibility
		{
			get
			{
				return (Visibility)this.GetValue(ExpandedGlyphVisibilityProperty);
			}
		}

		[Uno.NotImplemented]
		public Thickness Indentation
		{
			get
			{
				return (Thickness)this.GetValue(IndentationProperty);
			}
		}

		[Uno.NotImplemented]
		public static DependencyProperty CollapsedGlyphVisibilityProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"CollapsedGlyphVisibility", typeof(global::Windows.UI.Xaml.Visibility), 
			typeof(global::Windows.UI.Xaml.Controls.TreeViewItemTemplateSettings), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Visibility)));

		[Uno.NotImplemented]
		public static DependencyProperty DragItemsCountProperty { get; } =
		DependencyProperty.Register(
			"DragItemsCount", typeof(int), 
			typeof(TreeViewItemTemplateSettings), 
			new FrameworkPropertyMetadata(default(int)));

		[Uno.NotImplemented]
		public static DependencyProperty ExpandedGlyphVisibilityProperty { get; } =
		DependencyProperty.Register(
			"ExpandedGlyphVisibility", typeof(Visibility), 
			typeof(TreeViewItemTemplateSettings), 
			new FrameworkPropertyMetadata(default(Visibility)));

		[Uno.NotImplemented]
		public static DependencyProperty IndentationProperty { get; } =
		DependencyProperty.Register(
			"Indentation", typeof(Thickness), 
			typeof(TreeViewItemTemplateSettings), 
			new FrameworkPropertyMetadata(default(Thickness)));

		// Skipping already declared method Windows.UI.Xaml.Controls.TreeViewItemTemplateSettings.TreeViewItemTemplateSettings()
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItemTemplateSettings.TreeViewItemTemplateSettings()
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItemTemplateSettings.ExpandedGlyphVisibility.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItemTemplateSettings.CollapsedGlyphVisibility.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItemTemplateSettings.Indentation.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItemTemplateSettings.DragItemsCount.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItemTemplateSettings.ExpandedGlyphVisibilityProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItemTemplateSettings.CollapsedGlyphVisibilityProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItemTemplateSettings.IndentationProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItemTemplateSettings.DragItemsCountProperty.get
	}
}
