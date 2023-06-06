using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Runtime.CompilerServices;

namespace Uno.UI.RuntimeTests.Helpers
{
	public class ViewModelBase : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		protected void SetAndRaiseIfChanged<T>(ref T backingField, T value, [CallerMemberName] string propertyName = null)
		{
			if (!EqualityComparer<T>.Default.Equals(backingField, value))
			{
				backingField = value;
				RaisePropertyChanged(propertyName);
			}
		}

		internal void RaisePropertyChanged(string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
