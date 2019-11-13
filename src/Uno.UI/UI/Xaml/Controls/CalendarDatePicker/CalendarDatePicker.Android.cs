#if XAMARIN_ANDROID
using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls
{


	public partial class CalendarDatePicker : Control
	{
public  event Windows.Foundation.TypedEventHandler<CalendarDatePicker, CalendarDatePickerDateChangedEventArgs> DateChanged;

		public CalendarDatePicker()
		{
			DefaultStyleKey = typeof(CalendarDatePicker);
			InitializeVisualStates();
		}

#region DateProperty

		public Nullable<DateTimeOffset> Date
		{
			get { return (Nullable<DateTimeOffset>)this.GetValue(DateProperty); }
			set { this.SetValue(DateProperty, value); }
		}

		private string _dateString
		{
			get { return (string)this.GetValue(DateStringProperty); }
			set { this.SetValue(DateStringProperty, value); }
		}

		private static readonly DependencyProperty DateStringProperty =
	DependencyProperty.Register("_dateString", typeof(string), typeof(CalendarDatePicker), new PropertyMetadata("..."));

		public static DependencyProperty DateProperty { get; } =
	DependencyProperty.Register("Date", typeof(Nullable<DateTimeOffset>), typeof(CalendarDatePicker), new PropertyMetadata(DateTimeOffset.MinValue,
		(s, e) => ((CalendarDatePicker)s).OnDatePropertyChanged((Nullable<DateTimeOffset>)e.NewValue, (Nullable<DateTimeOffset>)e.OldValue)));

		private void OnDatePropertyChanged(Nullable<DateTimeOffset> newValue, Nullable<DateTimeOffset> oldValue)
		{

			string dateFormatString = "d";
			if (!this.DateFormat.IsNullOrEmpty())
			{
				dateFormatString = this.DateFormat;
			}

			if (!newValue.HasValue)
			{
				_dateString = this.PlaceholderText;
			}
			else
			{
				_dateString = newValue.Value.ToString(dateFormatString);
			}

			DateChanged?.Invoke(this, new CalendarDatePickerDateChangedEventArgs(newValue, oldValue));
		}

#endregion

#region PlaceholderText


		public string PlaceholderText
		{
			get { return (string)this.GetValue(PlaceholderTextProperty); }
			set { this.SetValue(PlaceholderTextProperty, value); }
		}

		public static global::Windows.UI.Xaml.DependencyProperty PlaceholderTextProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"PlaceholderText", typeof(string),
			typeof(global::Windows.UI.Xaml.Controls.CalendarDatePicker),
			new FrameworkPropertyMetadata(default(string)));

#endregion


#region DateFormat
		public string DateFormat
		{
			get { return (string)this.GetValue(DateFormatProperty); }
			set { this.SetValue(DateFormatProperty, value); }
		}

		public static global::Windows.UI.Xaml.DependencyProperty DateFormatProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"DateFormat", typeof(string),
			typeof(global::Windows.UI.Xaml.Controls.CalendarDatePicker),
			new FrameworkPropertyMetadata(""));


#endregion

#region MaxDateProperty
		public DateTimeOffset MaxDate
		{
			get { return (DateTimeOffset)this.GetValue(MaxDateProperty); }
			set { this.SetValue(MaxDateProperty, value); }
		}

		public static DependencyProperty MaxDateProperty { get; } =
			DependencyProperty.Register("MaxDate", typeof(DateTimeOffset), typeof(CalendarDatePicker),
			new PropertyMetadata(DateTimeOffset.MaxValue));

#endregion

#region MinDate
		public DateTimeOffset MinDate
		{
			get { return (DateTimeOffset)this.GetValue(MinDateProperty); }
			set { this.SetValue(MinDateProperty, value); }
		}

		public static DependencyProperty MinDateProperty { get; } =
			DependencyProperty.Register("MinDate", typeof(DateTimeOffset), typeof(CalendarDatePicker),
			new PropertyMetadata(DateTimeOffset.MinValue));

#endregion

#if XAMARIN

#region Template parts
		public const string FlyoutButtonPartName = "FlyoutButton";
#endregion

		private Button _flyoutButton;

		private bool _isLoaded;
		private bool _isViewReady;

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_flyoutButton = this.GetTemplateChild(FlyoutButtonPartName) as Button;

			var flyoutContent = _flyoutButton?.Content as IFrameworkElement;

			_isViewReady = true;

			SetupFlyoutButton();

			OnApplyTemplatePartial();

		}

		partial void OnApplyTemplatePartial();

		private protected override void OnLoaded()
		{
			base.OnLoaded();

			_isLoaded = true;

			var flyoutContent = _flyoutButton?.Content as IFrameworkElement;

			if (flyoutContent != null)
			{
				SetupFlyoutButton();
			}
		}

		private void SetupFlyoutButton()
		{
			if (!_isViewReady || !_isLoaded)
			{
				return;
			}

			if (_flyoutButton != null)
			{

				_flyoutButton.Flyout = new CalendarDatePickerFlyout()
				{
					MinDate = MinDate,
					MaxDate = MaxDate,
					Date = Date 
				};


				BindToFlyout(nameof(Date));
				BindToFlyout(nameof(MinDate));
				BindToFlyout(nameof(MaxDate));
			}
		}
#endif

		private void BindToFlyout(string propertyName)
		{
			this.Binding(propertyName, propertyName, _flyoutButton.Flyout, BindingMode.TwoWay);
		}

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new CalendarDatePickerAutomationPeer(this);
		}
	}
}
#endif
