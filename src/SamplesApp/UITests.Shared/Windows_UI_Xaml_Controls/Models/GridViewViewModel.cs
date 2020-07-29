using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Uno.UI.Samples.UITests.Helpers;
using Windows.UI.Core;
#if XAMARIN
using Windows.UI.Xaml.Controls;
#else
using Windows.UI.Xaml.Controls;
#endif

namespace Uno.UI.Samples.Presentation.SamplePages.GridView
{
	public class GridViewViewModel : ViewModelBase
	{

		public GridViewViewModel(CoreDispatcher coreDispatcher) : base(coreDispatcher)
		{
			SampleItems = GetSampleItems(coreDispatcher);
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

		private GridViewItemViewModel[] GetSampleItems(CoreDispatcher coreDispatcher)
		{
			var names = new[] { "Steve", "John", "Bob" };
			return names.Select(name =>  new GridViewItemViewModel(coreDispatcher,name)).ToArray();

		}
	}

	public class GridViewItemViewModel : ViewModelBase
	{
		public GridViewItemViewModel(CoreDispatcher coreDispatcher, string name) : base(coreDispatcher)
		{
			Name = name;
		}

		public string Name { get; }
	}

}
