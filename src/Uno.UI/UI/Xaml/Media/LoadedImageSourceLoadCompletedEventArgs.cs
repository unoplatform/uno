using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.UI.Xaml.Media
{
	public partial class LoadedImageSourceLoadCompletedEventArgs
	{
		private LoadedImageSourceLoadStatus _status;

		internal LoadedImageSourceLoadCompletedEventArgs(LoadedImageSourceLoadStatus status) => _status = status;

		public LoadedImageSourceLoadStatus Status { get => _status; }
	}
}
