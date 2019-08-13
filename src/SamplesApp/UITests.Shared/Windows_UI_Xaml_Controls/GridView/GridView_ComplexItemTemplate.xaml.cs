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
	[SampleControlInfo("GridView", nameof(GridView_ComplexItemTemplate), typeof(GridView_ComplexViewModel), ignoreInAutomatedTests: true)]
	public sealed partial class GridView_ComplexItemTemplate : UserControl
	{
		public GridView_ComplexItemTemplate()
		{
			this.InitializeComponent();
		}
	}

	public class GridView_ComplexViewModel : ViewModelBase
	{
		public GridView_ComplexItemViewModel[] SampleItems { get; }

		public GridView_ComplexViewModel(CoreDispatcher dispatcher) : base(dispatcher)
		{
			SampleItems = GetSampleItems();
		}


		private GridView_ComplexItemViewModel[] GetSampleItems()
		{
			return Enumerable
				.Range(0, 100)
				.Select(i => new GridView_ComplexItemViewModel
				{
					Client_FirstName = $"FirstName {i}",
					Client_LastName = $"LastName {i}",
					Client_Title = $"Title {i}",
					PatientName = $"PatientName {i}",
					StartTime = $"Time {i}",
					HasWellnessMembership = (i % 2) == 0,
					Reasons = Enumerable
						.Range(0, i % 3)
						.Select(r => $"Reason {r}")
						.ToArray(),
					AppointmentTypes = Enumerable
						.Range(0, i % 3)
						.Select(r => $"Appointment {r}")
						.ToArray()
				})
				.ToArray();
		}
	}

	public class GridView_ComplexItemViewModel : ViewModelBase
	{
		public GridView_ComplexItemViewModel(CoreDispatcher dispatcher = null) : base(dispatcher)
		{
		}

		public string PatientImage { get; set; }
		public string StartTime { get; set; }
		public string PatientName { get; set; }
		public bool HasWellnessMembership { get; set; }
		public string Client_Title { get; set; }

		public string Client_FirstName { get; set; }

		public string Client_LastName { get; set; }

		public string[] AppointmentTypes { get; set; }

		public string[] Reasons { get; set; }
	}
}
