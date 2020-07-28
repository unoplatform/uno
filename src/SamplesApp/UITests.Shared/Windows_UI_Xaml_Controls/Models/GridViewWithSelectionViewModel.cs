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
	public class GridViewWithSelectionViewModel : ViewModelBase
	{
		public GridViewWithSelectionViewModel()
		{
			Build(b => b
				.Properties(pb => pb
					.Attach("SampleItems", () => GetSampleItems())
					.Attach(SelectionMode, () => ListViewSelectionMode.Single)
					.AttachUserCommand("NoneSelection", async ct => SelectionMode.Value.OnNext(ListViewSelectionMode.None))
					.AttachUserCommand("SingleSelection", async ct => SelectionMode.Value.OnNext(ListViewSelectionMode.Single))
					.AttachUserCommand("MultipleSelection", async ct => SelectionMode.Value.OnNext(ListViewSelectionMode.Multiple))
				)
			);
		}

		private IDynamicProperty<ListViewSelectionMode> SelectionMode => this.GetProperty<ListViewSelectionMode>();


		private string[] GetSampleItems()
		{
			return Enumerable
				.Range(1, 10)
				.Select(i => i.ToString(CultureInfo.InvariantCulture))
				.ToArray();
		}
	}

}
