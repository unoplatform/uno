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
		public event EventHandler<CalendarDatePickerDateChangedEventArgs> DateChanged;

		public CalendarDatePicker()
		{
			InitializeVisualStates();
		}

		#region DateProperty

		public Nullable<DateTimeOffset> Date
		{
			get { return (DateTimeOffset?)this.GetValue(DateProperty); }
			set { this.SetValue(DateProperty, value); }
		}

		public string _DateString
		{
			get { return (string)this.GetValue(DateStringProperty); }
			set { this.SetValue(DateStringProperty, value); }
		}

		//public DateTimeOffset _DateValue
		//{
		//	get { return (DateTimeOffset)this.GetValue(DateValueProperty); }
		//	set { this.SetValue(DateValueProperty, value); }
		//}


		//#18331 If the Date property of DatePickerFlyout is two way binded, the ViewModel receives the control's default value while the ViewModel sends its default value which desynchronizes the values
		//Set initial value of DatePicker to DateTimeOffset.MinValue to avoid 2 way binding issue where the DatePicker reset Date(DateTimeOffset.MinValue) after the initial binding value.
		//We assume that this is the view model who will set the initial value just the time to fix #18331
		//public static DependencyProperty DateProperty =
		//DependencyProperty.Register(
		//	"Date", typeof(DateTimeOffset?),
		//	typeof(CalendarDatePicker),
		//	new FrameworkPropertyMetadata(default(DateTimeOffset?)));
		public static readonly DependencyProperty DateProperty =
	DependencyProperty.Register("Date", typeof(DateTimeOffset?), typeof(CalendarDatePicker), new PropertyMetadata(DateTimeOffset.MinValue,
		(s, e) => ((CalendarDatePicker)s).OnDatePropertyChanged((DateTimeOffset?)e.NewValue, (DateTimeOffset?)e.OldValue)));

	//	public static readonly DependencyProperty DateValueProperty =
	//DependencyProperty.Register("_DateValue", typeof(DateTimeOffset), typeof(CalendarDatePicker), new PropertyMetadata(DateTimeOffset.Now,
	//	(s, e) => ((CalendarDatePicker)s).OnDateValuePropertyChanged((DateTimeOffset)e.NewValue, (DateTimeOffset)e.OldValue)));

		public static readonly DependencyProperty DateStringProperty =
	DependencyProperty.Register("_DateString", typeof(string), typeof(CalendarDatePicker), new PropertyMetadata("..."));


		//private bool _inChanging = false;
		private void OnDatePropertyChanged(DateTimeOffset? newValue, DateTimeOffset? oldValue)
		{
			//if(_inChanging)
			//{
			//	return;
			//}

			//_inChanging = true;
			string strDateFormat = "d";
			if(this.DateFormat != default(string))
			{
				strDateFormat = this.DateFormat;
			}

			if(!newValue.HasValue)
			{
				_DateString = this.PlaceholderText;
				//_DateValue = DateTimeOffset.Now;
			}
			else
			{
				_DateString = newValue.Value.ToString(strDateFormat);
				//_DateValue = newValue.Value;
			}

			DateChanged?.Invoke(this, new CalendarDatePickerDateChangedEventArgs(newValue, oldValue));
			//_inChanging = false;
		}

		//private void OnDateValuePropertyChanged(DateTimeOffset newValue, DateTimeOffset oldValue)
		//{
		//	if (_inChanging)
		//	{
		//		return;
		//	}

		//	_inChanging = true;
		//	Date = newValue;

		//	string strDateFormat = "d";
		//	if (this.DateFormat != default(string))
		//	{
		//		strDateFormat = this.DateFormat;
		//	}

		//	_DateString = newValue.ToString(strDateFormat);

		//	DateChanged?.Invoke(this, new CalendarDatePickerDateChangedEventArgs(newValue, oldValue));
		//	_inChanging = false;
		//}


		#endregion

		#region PlaceholderText


		public string PlaceholderText
		{
			get { return (string)this.GetValue(PlaceholderTextProperty);	}
			set { this.SetValue(PlaceholderTextProperty, value);		}
		}

		public static global::Windows.UI.Xaml.DependencyProperty PlaceholderTextProperty  = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"PlaceholderText", typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.CalendarDatePicker), 
			new FrameworkPropertyMetadata(default(string)));

		#endregion


		#region DateFormat
		public  string DateFormat
		{
			get { return (string)this.GetValue(DateFormatProperty);		}
			set { this.SetValue(DateFormatProperty, value);			}
		}

		public static global::Windows.UI.Xaml.DependencyProperty DateFormatProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"DateFormat", typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.CalendarDatePicker), 
			new FrameworkPropertyMetadata(default(string)));


		#endregion

		#region MaxDateProperty
		public DateTimeOffset MaxDate
		{
			get { return (DateTimeOffset)this.GetValue(MaxDateProperty); }
			set { this.SetValue(MaxDateProperty, value); }
		}

		public static DependencyProperty MaxDateProperty =
			DependencyProperty.Register("MaxDate", typeof(DateTimeOffset), typeof(CalendarDatePicker),
			new PropertyMetadata(DateTimeOffset.MaxValue));

		#endregion

		#region MinDate
		public DateTimeOffset MinDate
		{
			get { return (DateTimeOffset)this.GetValue(MinDateProperty); }
			set { this.SetValue(MinDateProperty, value); }
		}

		public static DependencyProperty MinDateProperty =
			DependencyProperty.Register("MinDate", typeof(DateTimeOffset), typeof(CalendarDatePicker),
			new PropertyMetadata(DateTimeOffset.MinValue));

		//public static readonly DependencyProperty MinYearProperty =
		//	DependencyProperty.Register("MinYear", typeof(DateTimeOffset), typeof(CalendarDatePicker), new PropertyMetadata(DateTimeOffset.MinValue,
		//		(s, e) => ((CalendarDatePicker)s).OnMinYearChangedPartial()));

		//partial void OnMinYearChangedPartial();
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

		protected override void OnLoaded()
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
					Date = Date // this.Date.GetValueOrDefault(DateTimeOffset.Now)
			};


				BindToFlyout(nameof(Date));
				BindToFlyout(nameof(MinDate));
				BindToFlyout(nameof(MaxDate));
#endif
			}
		}

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

