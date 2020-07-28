using nVentive.Umbrella.Presentation.Light;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
#if XAMARIN
using Windows.UI.Xaml.Controls;
#else
using Windows.UI.Xaml.Controls;
#endif

namespace Uno.UI.Samples.Presentation.SamplePages.GridView
{
	public class GridViewViewModel : ViewModelBase
	{
		public GridViewViewModel()
		{
			Build(b => b
				.Properties(pb => pb
					.Attach("SampleItems", () => GetSampleItems())
					.Attach("MyBool", () => true)
				)
			);
		}
		

		private GridViewItemViewModel[] GetSampleItems()
		{
			var names = new[] {"Steve", "John", "Bob"};
			return names.Select(name => this.CreateItemViewModel(() => new GridViewItemViewModel(name))).ToArray();

		}
	}

	public class GridViewItemViewModel : ViewModelBase
	{
		public GridViewItemViewModel(string name)
		{
			Build(b => b
				.Properties(pb => pb
					.Attach("Name", () => name)
				)
			);
		}
	
	}

}