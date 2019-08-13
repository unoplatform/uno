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
	[SampleControlInfo("GridView", nameof(GridView_Vertical_MaxItemWidth), typeof(GridView_Vertical_MaxItemWidthViewModel))]
	public sealed partial class GridView_Vertical_MaxItemWidth : UserControl
	{
		public GridView_Vertical_MaxItemWidth()
		{
			this.InitializeComponent();
		}
	}

	public class GridView_Vertical_MaxItemWidthViewModel : ViewModelBase
	{
		public string[] SampleItems { get; }

		public GridView_Vertical_MaxItemWidthViewModel(CoreDispatcher dispatcher) : base(dispatcher)
		{
			SampleItems = new string[37] {
				"acaagatgcc", "attgtccccc", "ggcctcctgc", "tgctgctgct", "ctccggggcc", "acggccaccg",
				"ctgccctgcc", "cctggagggt", "ggccccaccg", "gccgagacag", "cgagcatatg", "caggaagcgg",
				"caggaataag", "gaaaagcagc", "ctcctgactt", "tcctcgcttg", "gtggtttgag", "tggacctccc",
				"aggccagtgc", "cgggcccctc", "ataggagagg", "aagctcggga", "ggtggccagg", "cggcaggaag",
				"gcgcaccccc", "ccagcaatcc", "gcgcgccggg", "acagaatgcc", "ctgcaggaac", "ttcttctgga",
				"agaccttctc", "ctcctgcaaa", "taaaacctca", "cccatgaatg", "ctcacgcaag", "tttaattaca",
				"gacctgaatc",
			};
		}
	}
}
