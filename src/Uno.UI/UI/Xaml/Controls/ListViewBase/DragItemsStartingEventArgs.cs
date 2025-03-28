#nullable enable

using System.Collections.Generic;
using Windows.ApplicationModel.DataTransfer;

namespace Windows.UI.Xaml.Controls
{
	public partial class DragItemsStartingEventArgs
	{
		private readonly DragStartingEventArgs? _inner;

		public DragItemsStartingEventArgs()  // Part of the public API. DO NOT USE internaly
		{
			Data = new DataPackage();
			Items = new List<object>();
		}

		internal DragItemsStartingEventArgs(DragStartingEventArgs inner, IList<object> items)
		{
			_inner = inner;

			Data = _inner.Data;
			Items = items;
		}

		public bool Cancel
		{
			get => _inner?.Cancel ?? false;
			set
			{
				if (_inner is { })
				{
					_inner.Cancel = value;
				}
			}
		}

		public DataPackage Data { get; }
		public IList<object> Items { get; }
	}
}
