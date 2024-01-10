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
	internal class GridView_Vertical_MaxItemWidthViewModel : ViewModelBase
	{
		public GridView_Vertical_MaxItemWidthViewModel(Private.Infrastructure.UnitTestDispatcherCompat coreDispatcher) : base(coreDispatcher)
		{
		}

		public object SampleItems { get; } = Enumerable.Range(1, 10).ToArray();

		private int[] GetSampleItems(CancellationToken ct)
		{
			return Enumerable
				.Range(1, 10)
				.ToArray();
		}
	}
}
