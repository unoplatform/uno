using nVentive.Umbrella.Presentation.Light;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.UI.Samples.Presentation.SamplePages
{
    public class GridView_Vertical_MaxItemWidthViewModel : ViewModelBase
	{
		public GridView_Vertical_MaxItemWidthViewModel()
		{
			Build(b => b
				.Properties(pb => pb
					.Attach("SampleItems", GetSampleItems)
				)
			);
		}

		private async Task<int[]> GetSampleItems(CancellationToken ct)
		{
			return Enumerable
				.Range(1, 10)
				.ToArray();
		}
	}
}
