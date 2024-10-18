using System;
using System.Collections.Generic;
using System.ComponentModel;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using System.Text;
using Android.Widget;
using Uno.UI.DataBinding;
using Windows.UI.Xaml;

namespace Uno.UI.Controls
{
	public partial class BindableRadioButton : AndroidX.AppCompat.Widget.AppCompatRadioButton, DependencyObject, INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		public BindableRadioButton()
			: base(ContextHelper.Current)
		{
			InitializeBinder();
		}

		public override bool Enabled
		{
			get
			{
				return base.Enabled;
			}
			set
			{
				if (base.Enabled != value)
				{
					base.Enabled = value;
					RaisePropertyChanged();
				}
			}
		}

		public override bool Checked
		{
			get
			{
				return base.Checked;
			}
			set
			{
				if (base.Checked != value)
				{
					base.Checked = value;
					RaisePropertyChanged();
				}
			}
		}

		protected void RaisePropertyChanged([CallerMemberName] string propertyName = null)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}
}
