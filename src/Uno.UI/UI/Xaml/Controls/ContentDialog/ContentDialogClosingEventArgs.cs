using System;
using Uno.Helpers;

namespace Windows.UI.Xaml.Controls
{
	public partial class ContentDialogClosingEventArgs
	{
		internal ContentDialogClosingEventArgs(Action<ContentDialogClosingEventArgs> complete, ContentDialogResult result)
		{
			DeferralManager = new DeferralManager<ContentDialogClosingDeferral>(h => new ContentDialogClosingDeferral(h));
			DeferralManager.Completed += (s, e) => complete(this);

			Result = result;
		}

		internal DeferralManager<ContentDialogClosingDeferral> DeferralManager { get; }

		public bool Cancel { get; set; }

		public ContentDialogResult Result { get; }

		public ContentDialogClosingDeferral GetDeferral() => DeferralManager.GetDeferral();
	}
}
