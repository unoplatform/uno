using nVentive.Umbrella.Extensions;
using nVentive.Umbrella.Presentation.Light;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
#if XAMARIN
using nVentive.Umbrella.Views.UI.Xaml.Controls;
#else
using Windows.UI.Xaml.Controls;
#endif

namespace Uno.UI.Samples.Content.UITests.GridView
{
	public class GridView_ComplexViewModel : ViewModelBase
	{
		public GridView_ComplexViewModel()
		{
			Build(b => b
				.Properties(pb => pb
					.Attach("SampleItems", () => GetSampleItems())
				)
			);
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
		public GridView_ComplexItemViewModel()
		{
			Build(b => b
				.Properties(pb => pb
				)
			);
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