using Android.Content;
using Android.Views;
using Android.Widget;
using Java.Lang;
using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Java.Lang.Reflect;
using Uno.Foundation.Logging;

namespace Uno.UI.Controls
{
	public partial class BindableDatePicker : DatePicker
	{
		public BindableDatePicker(Context c) : base(c)
		{
			LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);

#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CA1422 // Validate platform compatibility
			CalendarViewShown = false;
#pragma warning restore CA1422 // Validate platform compatibility
#pragma warning restore CS0618 // Type or member is obsolete
		}

		ViewStates _monthVisibility;
		public ViewStates MonthVisibility
		{
			get { return _monthVisibility; }
			set
			{
				if (_monthVisibility != value)
				{
					_monthVisibility = value;
					SetVisibility(value, "mMonthPicker", "mMonthSpinner");
				}
			}
		}

		ViewStates _yearVisibility;
		public ViewStates YearVisibility
		{
			get { return _yearVisibility; }
			set
			{
				if (_yearVisibility != value)
				{
					_yearVisibility = value;
					SetVisibility(value, "mYearPicker", "mYearSpinner");
				}
			}
		}

		ViewStates _dayVisibility;
		public ViewStates DayVisibility
		{
			get { return _dayVisibility; }
			set
			{
				if (_dayVisibility != value)
				{
					_dayVisibility = value;
					SetVisibility(value, "mDayPicker", "mDaySpinner");
				}
			}
		}

		private Field[] _declaredFields;
		public Field[] DeclaredFields
		{
			get
			{
				if (_declaredFields == null)
				{
					_declaredFields = Class.FromType(typeof(DatePicker)).GetDeclaredFields();
				}
				return _declaredFields;

			}
		}

		private void SetVisibility(ViewStates visibility, params string[] memberNames)
		{
			try
			{//http://stackoverflow.com/questions/10401915/hide-year-field-in-android-datepicker
				var fields = DeclaredFields;
				foreach (var field in fields)
				{
					if (memberNames.Contains(field.Name))
					{
						field.Accessible = true;
						var picker = (View)field.Get(this);
						picker.Visibility = visibility;
					}
				}
			}
			catch (SecurityException)
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Error))
				{
					this.Log().Error("Can't hide " + string.Join(", ", memberNames));
				}
			}
			catch (IllegalArgumentException)
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Error))
				{
					this.Log().Error("Can't hide " + string.Join(", ", memberNames));
				}
			}
			catch (IllegalAccessException)
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Error))
				{
					this.Log().Error("Can't hide " + string.Join(", ", memberNames));
				}
			}
		}
	}
}
