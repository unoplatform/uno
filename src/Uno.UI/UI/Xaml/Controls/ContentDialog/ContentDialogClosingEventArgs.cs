using System;

namespace Windows.UI.Xaml.Controls
{
	public  partial class ContentDialogClosingEventArgs 
	{
		private readonly Action<ContentDialogClosingEventArgs> _complete;

		internal ContentDialogClosingEventArgs(Action<ContentDialogClosingEventArgs> complete)
		{
			_complete = complete;
		}
		internal ContentDialogClosingDeferral Deferral { get; private set; }

		public bool Cancel { get; set; }

		public ContentDialogResult Result { get; }

		public ContentDialogClosingDeferral GetDeferral()
			=> Deferral = new ContentDialogClosingDeferral(() => _complete(this));
	}
}
