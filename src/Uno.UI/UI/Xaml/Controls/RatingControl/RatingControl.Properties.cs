using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;

namespace Windows.UI.Xaml.Controls
{
	//[PortStatus(Complete = true)]
	public partial class RatingControl
	{
		[PortStatus("From Generated/3.x", Complete = true)]
		public double Value
		{
			get
			{
				return (double)this.GetValue(ValueProperty);
			}
			set
			{
				this.SetValue(ValueProperty, value);
			}
		}

		[PortStatus("From Generated/3.x", Complete = true)]
		public double PlaceholderValue
		{
			get
			{
				return (double)this.GetValue(PlaceholderValueProperty);
			}
			set
			{
				this.SetValue(PlaceholderValueProperty, value);
			}
		}

		[PortStatus("From Generated/3.x", Complete = true)]
		public int MaxRating
		{
			get
			{
				return (int)this.GetValue(MaxRatingProperty);
			}
			set
			{
				this.SetValue(MaxRatingProperty, value);
			}
		}

		[PortStatus("From Generated/3.x", Complete = true)]
		public RatingItemInfo ItemInfo
		{
			get
			{
				return (RatingItemInfo)this.GetValue(ItemInfoProperty);
			}
			set
			{
				this.SetValue(ItemInfoProperty, value);
			}
		}

		[PortStatus("From Generated/3.x", Complete = true)]
		public bool IsReadOnly
		{
			get
			{
				return (bool)this.GetValue(IsReadOnlyProperty);
			}
			set
			{
				this.SetValue(IsReadOnlyProperty, value);
			}
		}
		[PortStatus("From Generated/3.x", Complete = true)]
		public bool IsClearEnabled
		{
			get
			{
				return (bool)this.GetValue(IsClearEnabledProperty);
			}
			set
			{
				this.SetValue(IsClearEnabledProperty, value);
			}
		}

		[PortStatus("From Generated/3.x", Complete = true)]
		public int InitialSetValue
		{
			get
			{
				return (int)this.GetValue(InitialSetValueProperty);
			}
			set
			{
				this.SetValue(InitialSetValueProperty, value);
			}
		}

		[PortStatus("From Generated/3.x", Complete = true)]
		public string Caption
		{
			get
			{
				return (string)this.GetValue(CaptionProperty);
			}
			set
			{
				this.SetValue(CaptionProperty, value);
			}
		}

		[PortStatus("From Generated/3.x", Complete = true)]
		public static DependencyProperty CaptionProperty { get; } =
			DependencyProperty.Register(
				"Caption", typeof(string),
				typeof(RatingControl),
				new FrameworkPropertyMetadata(null, OnStaticPropertyChanged));

		[PortStatus("From Generated/3.x", Complete = true)]
		public static DependencyProperty InitialSetValueProperty { get; } =
			DependencyProperty.Register(
				"InitialSetValue", typeof(int),
				typeof(RatingControl),
				new FrameworkPropertyMetadata(1, OnStaticPropertyChanged));

		[PortStatus("From Generated/3.x", Complete = true)]
		public static DependencyProperty IsClearEnabledProperty { get; } =
			DependencyProperty.Register(
				"IsClearEnabled", typeof(bool),
				typeof(RatingControl),
				new FrameworkPropertyMetadata(true, OnStaticPropertyChanged));

		[PortStatus("From Generated/3.x", Complete = true)]
		public static DependencyProperty IsReadOnlyProperty { get; } =
			DependencyProperty.Register(
				"IsReadOnly", typeof(bool),
				typeof(RatingControl),
				new FrameworkPropertyMetadata(false, OnStaticPropertyChanged));

		public static DependencyProperty ItemInfoProperty { get; } =
			DependencyProperty.Register(
				"ItemInfo", typeof(RatingItemInfo),
				typeof(RatingControl),
				new FrameworkPropertyMetadata(null, OnStaticPropertyChanged));

		[PortStatus("From Generated/3.x", Complete = true)]
		public static DependencyProperty MaxRatingProperty { get; } =
			DependencyProperty.Register(
				"MaxRating", typeof(int),
				typeof(RatingControl),
				new FrameworkPropertyMetadata(5, OnStaticPropertyChanged));

		[PortStatus("From Generated/3.x", Complete = true)]
		public static DependencyProperty PlaceholderValueProperty { get; } =
			DependencyProperty.Register(
				"PlaceholderValue", typeof(double),
				typeof(RatingControl),
				new FrameworkPropertyMetadata(-1.0, OnStaticPropertyChanged));

		[PortStatus("From Generated/3.x", Complete = true)]
		public static DependencyProperty ValueProperty { get; } =
			DependencyProperty.Register(
				"Value", typeof(double),
				typeof(RatingControl),
				new FrameworkPropertyMetadata(-1.0, OnStaticPropertyChanged));
	}
}
