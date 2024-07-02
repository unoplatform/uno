using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	public partial class Pivot : ItemsControl
	{
		public DataTemplate TitleTemplate
		{
			get
			{
				return (DataTemplate)this.GetValue(TitleTemplateProperty);
			}
			set
			{
				this.SetValue(TitleTemplateProperty, value);
			}
		}

		public object Title
		{
			get
			{
				return (object)this.GetValue(TitleProperty);
			}
			set
			{
				this.SetValue(TitleProperty, value);
			}
		}

		public object SelectedItem
		{
			get
			{
				return (object)this.GetValue(SelectedItemProperty);
			}
			set
			{
				this.SetValue(SelectedItemProperty, value);
			}
		}

		public int SelectedIndex
		{
			get
			{
				return (int)this.GetValue(SelectedIndexProperty);
			}
			set
			{
				this.SetValue(SelectedIndexProperty, value);
			}
		}

		public DataTemplate HeaderTemplate
		{
			get
			{
				return (DataTemplate)this.GetValue(HeaderTemplateProperty);
			}
			set
			{
				this.SetValue(HeaderTemplateProperty, value);
			}
		}

		public DataTemplate RightHeaderTemplate
		{
			get
			{
				return (DataTemplate)this.GetValue(RightHeaderTemplateProperty);
			}
			set
			{
				this.SetValue(RightHeaderTemplateProperty, value);
			}
		}

		public object RightHeader
		{
			get
			{
				return (object)this.GetValue(RightHeaderProperty);
			}
			set
			{
				this.SetValue(RightHeaderProperty, value);
			}
		}

		public DataTemplate LeftHeaderTemplate
		{
			get
			{
				return (DataTemplate)this.GetValue(LeftHeaderTemplateProperty);
			}
			set
			{
				this.SetValue(LeftHeaderTemplateProperty, value);
			}
		}

		public object LeftHeader
		{
			get
			{
				return (object)this.GetValue(LeftHeaderProperty);
			}
			set
			{
				this.SetValue(LeftHeaderProperty, value);
			}
		}


		public static DependencyProperty HeaderTemplateProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"HeaderTemplate", typeof(DataTemplate),
			typeof(Pivot),
			new FrameworkPropertyMetadata(default(DataTemplate), options: FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));

		public static DependencyProperty IsLockedProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsLocked", typeof(bool),
			typeof(Pivot),
			new FrameworkPropertyMetadata(default(bool)));

		public static DependencyProperty SelectedIndexProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"SelectedIndex", typeof(int),
			typeof(Pivot),
			new FrameworkPropertyMetadata(
				defaultValue: -1,
				options: FrameworkPropertyMetadataOptions.None,
				propertyChangedCallback: (s, e) => (s as Pivot)?.OnSelectedIndexChanged((int)e.OldValue, (int)e.NewValue)
			)
		);

		public static DependencyProperty SelectedItemProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"SelectedItem", typeof(object),
			typeof(Pivot),
			new FrameworkPropertyMetadata(
				defaultValue: default(object),
				propertyChangedCallback: (s, e) => (s as Pivot)?.OnSelectedItemPropertyChanged(e.OldValue, e.NewValue)
				)
			);

		public static DependencyProperty TitleProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"Title", typeof(object),
			typeof(Pivot),
			new FrameworkPropertyMetadata(
				defaultValue: default(object),
				options: FrameworkPropertyMetadataOptions.None,
				propertyChangedCallback: OnTitlePropertyChanged
			)
		);

		public static DependencyProperty TitleTemplateProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"TitleTemplate", typeof(DataTemplate),
			typeof(Pivot),
			new FrameworkPropertyMetadata(default(DataTemplate), options: FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));

		public static DependencyProperty LeftHeaderProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"LeftHeader", typeof(object),
			typeof(Pivot),
			new FrameworkPropertyMetadata(default(object)));

		public static DependencyProperty LeftHeaderTemplateProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"LeftHeaderTemplate", typeof(DataTemplate),
			typeof(Pivot),
			new FrameworkPropertyMetadata(default(DataTemplate), options: FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));

		public static DependencyProperty RightHeaderProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"RightHeader", typeof(object),
			typeof(Pivot),
			new FrameworkPropertyMetadata(default(object)));

		public static DependencyProperty RightHeaderTemplateProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"RightHeaderTemplate", typeof(DataTemplate),
			typeof(Pivot),
			new FrameworkPropertyMetadata(default(DataTemplate), options: FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));

#pragma warning disable 67
		public event TypedEventHandler<Pivot, PivotItemEventArgs> PivotItemLoaded;

		public event TypedEventHandler<Pivot, PivotItemEventArgs> PivotItemLoading;

		public event TypedEventHandler<Pivot, PivotItemEventArgs> PivotItemUnloaded;

		public event TypedEventHandler<Pivot, PivotItemEventArgs> PivotItemUnloading;

		public event SelectionChangedEventHandler SelectionChanged;
	}
}
