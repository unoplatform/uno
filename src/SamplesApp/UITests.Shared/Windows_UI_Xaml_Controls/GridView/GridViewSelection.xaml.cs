using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.GridView
{
	[SampleControlInfo("GridView", nameof(GridViewSelection), typeof(GridViewWithSelectionViewModel))]
	public sealed partial class GridViewSelection : UserControl
	{
		public GridViewSelection()
		{
			this.InitializeComponent();
		}
	}

	public class GridViewWithSelectionViewModel : ViewModelBase
	{
		public ListViewSelectionMode SelectionMode { get; }
		public ListViewSelectionMode NoneSelection { get; }
		public ListViewSelectionMode SingleSelection { get; }
		public ListViewSelectionMode MultipleSelection { get; }

		public string[] SampleItems { get; }

		public GridViewWithSelectionViewModel(CoreDispatcher dispatcher) : base(dispatcher)
		{
			SampleItems = _sampleItems;

			SelectionMode = ListViewSelectionMode.Single;

			NoneSelection = ListViewSelectionMode.None;
			SingleSelection = ListViewSelectionMode.Single;
			MultipleSelection = ListViewSelectionMode.Multiple;
		}

		private static readonly string[] _sampleItems = {"1", "2", "3", "4", "5", "6","7", "8", "9", "10"};
	}
}
