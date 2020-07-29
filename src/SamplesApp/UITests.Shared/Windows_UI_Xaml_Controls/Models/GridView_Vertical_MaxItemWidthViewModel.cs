using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI.Samples.UITests.Helpers;
using Windows.UI.Core;

namespace Uno.UI.Samples.Presentation.SamplePages
{
	public class GridView_Vertical_MaxItemWidthViewModel : ViewModelBase
	{
		public GridView_Vertical_MaxItemWidthViewModel(CoreDispatcher coreDispatcher) : base(coreDispatcher)
		{
		}

		public object SampleItems { get; } = Enumerable.Range(1, 10).ToArray();

		private async Task<int[]> GetSampleItems(CancellationToken ct)
		{
			return Enumerable
				.Range(1, 10)
				.ToArray();
		}
	}
}
