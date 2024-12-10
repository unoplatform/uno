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
	internal class GridViewWithSelectionViewModel : ViewModelBase
	{

		public GridViewWithSelectionViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
			// TODO: restore commands
			//Build(b => b
			//	.Properties(pb => pb
			//		.Attach("SampleItems", () => GetSampleItems())
			//		.Attach(SelectionMode, () => ListViewSelectionMode.Single)
			//		.AttachUserCommand("NoneSelection", async ct => SelectionMode.Value.OnNext(ListViewSelectionMode.None))
			//		.AttachUserCommand("SingleSelection", async ct => SelectionMode.Value.OnNext(ListViewSelectionMode.Single))
			//		.AttachUserCommand("MultipleSelection", async ct => SelectionMode.Value.OnNext(ListViewSelectionMode.Multiple))
			//	)
			//);

			SampleItems = GetSampleItems();
		}

		public object SampleItems { get; }

		private ListViewSelectionMode _selectionMode;
		public ListViewSelectionMode SelectionMode
		{
			get => _selectionMode;
			set
			{
				_selectionMode = value;
				RaisePropertyChanged();
			}
		}


		private string[] GetSampleItems()
		{
			return Enumerable
				.Range(1, 10)
				.Select(i => i.ToString(CultureInfo.InvariantCulture))
				.ToArray();
		}
	}

}
