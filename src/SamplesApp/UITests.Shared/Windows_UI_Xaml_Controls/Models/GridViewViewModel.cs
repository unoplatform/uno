using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Uno.UI.Samples.UITests.Helpers;
using Windows.UI.Core;
using Private.Infrastructure;

#if XAMARIN
using Windows.UI.Xaml.Controls;
#else
using Windows.UI.Xaml.Controls;
#endif

namespace Uno.UI.Samples.Presentation.SamplePages.GridView
{
	internal class GridViewViewModel : ViewModelBase
	{

		public GridViewViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
			SampleItems = GetSampleItems(dispatcher);
		}

		public object SampleItems { get; }

		private bool _myBool = true;
		public bool MyBool
		{
			get => _myBool;
			set
			{
				_myBool = value;
				RaisePropertyChanged();
			}
		}

		private GridViewItemViewModel[] GetSampleItems(UnitTestDispatcherCompat dispatcher)
		{
			var names = new[] { "Steve", "John", "Bob" };
			return names.Select(name => new GridViewItemViewModel(dispatcher, name)).ToArray();

		}
	}

	internal class GridViewItemViewModel : ViewModelBase
	{
		public GridViewItemViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher, string name) : base(dispatcher)
		{
			Name = name;
		}

		public string Name { get; }
	}

}
