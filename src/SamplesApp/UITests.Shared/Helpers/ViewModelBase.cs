using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using Windows.UI.Core;

namespace Uno.UI.Samples.UITests.Helpers
{
	public class ViewModelBase : INotifyPropertyChanged
	{
		public CoreDispatcher Dispatcher { get; }

		public event PropertyChangedEventHandler PropertyChanged;

		public ViewModelBase(CoreDispatcher dispatcher)
		{
			Dispatcher = dispatcher;
		}

		protected void RaisePropertyChanged([CallerMemberName] string propertyName = "")
		{
			var unused = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
			});
		}
	}
}
