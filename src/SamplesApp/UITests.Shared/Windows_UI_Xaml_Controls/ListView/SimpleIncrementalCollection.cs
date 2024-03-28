using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Data;

namespace SamplesApp.Windows_UI_Xaml_Controls.ListView
{
	public class SimpleIncrementalCollection<T> : ObservableCollection<T>, ISupportIncrementalLoading
	{
		private readonly Func<int, T> _generator;

		public SimpleIncrementalCollection(Func<int, T> generator)
		{
			_generator = generator;
		}

		public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
		{
			return AsyncInfo.Run(async ct =>
			{
				await Task.Delay(25);
				for (int i = 0; i < count; i++)
				{
					var newItem = _generator(Count);
					Add(newItem);
				}
				return new LoadMoreItemsResult { Count = count };
			});
		}

		public bool HasMoreItems { get; set; } = true;
	}
}
