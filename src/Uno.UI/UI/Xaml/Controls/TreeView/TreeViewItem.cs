#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using System;
using Windows.UI.Xaml.Media;
using Windows.Foundation.Metadata;

namespace Windows.UI.Xaml.Controls
{

	[Uno.NotImplemented]
	public partial class TreeViewItem : global::Windows.UI.Xaml.Controls.ListViewItem
	{
		[Uno.NotImplemented]
		public bool IsExpanded
		{
			get
			{
				return (bool)this.GetValue(IsExpandedProperty);
			}
			set
			{
				this.SetValue(IsExpandedProperty, value);
			}
		}
		[Uno.NotImplemented]
		public double GlyphSize
		{
			get
			{
				return (double)this.GetValue(GlyphSizeProperty);
			}
			set
			{
				this.SetValue(GlyphSizeProperty, value);
			}
		}

		[Uno.NotImplemented]
		public double GlyphOpacity
		{
			get
			{
				return (double)this.GetValue(GlyphOpacityProperty);
			}
			set
			{
				this.SetValue(GlyphOpacityProperty, value);
			}
		}

		[Uno.NotImplemented]
		public Brush GlyphBrush
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Brush)this.GetValue(GlyphBrushProperty);
			}
			set
			{
				this.SetValue(GlyphBrushProperty, value);
			}
		}

		[Uno.NotImplemented]
		public string ExpandedGlyph
		{
			get
			{
				return (string)this.GetValue(ExpandedGlyphProperty);
			}
			set
			{
				this.SetValue(ExpandedGlyphProperty, value);
			}
		}

		[Uno.NotImplemented]
		public string CollapsedGlyph
		{
			get
			{
				return (string)this.GetValue(CollapsedGlyphProperty);
			}
			set
			{
				this.SetValue(CollapsedGlyphProperty, value);
			}
		}

		[Uno.NotImplemented]
		public TreeViewItemTemplateSettings TreeViewItemTemplateSettings
		{
			get
			{
				return (TreeViewItemTemplateSettings)this.GetValue(TreeViewItemTemplateSettingsProperty);
			}
		}

		[Uno.NotImplemented]
		public object ItemsSource
		{
			get
			{
				return (object)this.GetValue(ItemsSourceProperty);
			}
			set
			{
				this.SetValue(ItemsSourceProperty, value);
			}
		}

		[Uno.NotImplemented]
		public bool HasUnrealizedChildren
		{
			get
			{
				return (bool)this.GetValue(HasUnrealizedChildrenProperty);
			}
			set
			{
				this.SetValue(HasUnrealizedChildrenProperty, value);
			}
		}

		[Uno.NotImplemented]
		public static DependencyProperty CollapsedGlyphProperty { get; } =
		DependencyProperty.Register(
			"CollapsedGlyph", typeof(string), 
			typeof(TreeViewItem), 
			new FrameworkPropertyMetadata(default(string)));

		[Uno.NotImplemented]
		public static DependencyProperty ExpandedGlyphProperty { get; } =
		DependencyProperty.Register(
			"ExpandedGlyph", typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.TreeViewItem), 
			new FrameworkPropertyMetadata(default(string)));

		[Uno.NotImplemented]
		public static DependencyProperty GlyphBrushProperty { get; } =
		DependencyProperty.Register(
			"GlyphBrush", typeof(Brush), 
			typeof(TreeViewItem), 
			new FrameworkPropertyMetadata(default(Brush)));

		[Uno.NotImplemented]
		public static DependencyProperty GlyphOpacityProperty { get; } =
		DependencyProperty.Register(
			"GlyphOpacity", typeof(double), 
			typeof(TreeViewItem), 
			new FrameworkPropertyMetadata(default(double)));

		[Uno.NotImplemented]
		public static DependencyProperty GlyphSizeProperty { get; } =
		DependencyProperty.Register(
			"GlyphSize", typeof(double), 
			typeof(TreeViewItem), 
			new FrameworkPropertyMetadata(default(double)));

		[Uno.NotImplemented]
		public static DependencyProperty IsExpandedProperty { get; } =
		DependencyProperty.Register(
			"IsExpanded", typeof(bool), 
			typeof(TreeViewItem), 
			new FrameworkPropertyMetadata(default(bool)));

		[Uno.NotImplemented]
		public static DependencyProperty TreeViewItemTemplateSettingsProperty { get; } =
		DependencyProperty.Register(
			"TreeViewItemTemplateSettings", typeof(TreeViewItemTemplateSettings), 
			typeof(TreeViewItem), 
			new FrameworkPropertyMetadata(default(TreeViewItemTemplateSettings)));

		[Uno.NotImplemented]
		public static DependencyProperty HasUnrealizedChildrenProperty { get; } =
		DependencyProperty.Register(
			"HasUnrealizedChildren", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.TreeViewItem), 
			new FrameworkPropertyMetadata(default(bool)));

		[Uno.NotImplemented]
		public static DependencyProperty ItemsSourceProperty { get; } =
		DependencyProperty.Register(
			"ItemsSource", typeof(object), 
			typeof(global::Windows.UI.Xaml.Controls.TreeViewItem), 
			new FrameworkPropertyMetadata(default(object)));
		
		[Uno.NotImplemented]
		public TreeViewItem() : base()
		{
			ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TreeViewItem", "TreeViewItem.TreeViewItem()");
		}
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItem.TreeViewItem()
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItem.GlyphOpacity.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItem.GlyphOpacity.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItem.GlyphBrush.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItem.GlyphBrush.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItem.ExpandedGlyph.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItem.ExpandedGlyph.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItem.CollapsedGlyph.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItem.CollapsedGlyph.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItem.GlyphSize.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItem.GlyphSize.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItem.IsExpanded.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItem.IsExpanded.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItem.TreeViewItemTemplateSettings.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItem.HasUnrealizedChildren.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItem.HasUnrealizedChildren.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItem.ItemsSource.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItem.ItemsSource.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItem.HasUnrealizedChildrenProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItem.ItemsSourceProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItem.GlyphOpacityProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItem.GlyphBrushProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItem.ExpandedGlyphProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItem.CollapsedGlyphProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItem.GlyphSizeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItem.IsExpandedProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItem.TreeViewItemTemplateSettingsProperty.get
	}
}
