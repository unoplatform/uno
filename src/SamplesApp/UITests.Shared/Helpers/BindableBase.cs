using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace UITests.Shared.Helpers
{
	[Windows.UI.Xaml.Data.Bindable]
	public class BindableBase : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		protected void RaiseAllPropertiesChanged()
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
		}
	}
}
