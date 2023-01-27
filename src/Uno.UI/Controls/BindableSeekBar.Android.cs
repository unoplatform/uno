using Android.Widget;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;

namespace Uno.UI.Controls
{
	public partial class BindableSeekBar : SeekBar, DependencyObject
	{
		private const float NativeResolution = 1000;

		public BindableSeekBar()
			: base(ContextHelper.Current)
		{
			ProgressChanged += (s, e) =>
			{
				_value = Progress / NativeResolution + _minimum;
				SetBindingValue(_value, nameof(Value));
			};
		}

		private float _maximum;

		public float Maximum
		{
			get { return _maximum; }
			set
			{
				_maximum = value;

				UpdateNativeMax();
			}
		}

		private float _minimum;

		public float Minimum
		{
			get { return _minimum; }
			set
			{
				_minimum = value;

				UpdateNativeMax();
			}
		}

		private float _value;

		public float Value
		{
			get { return _value; }
			set
			{
				_value = value;
				Progress = (int)((_value - _minimum) * NativeResolution);
			}
		}

		private void UpdateNativeMax()
		{
			Max = (int)((_maximum - _minimum) * NativeResolution);
		}
	}
}
