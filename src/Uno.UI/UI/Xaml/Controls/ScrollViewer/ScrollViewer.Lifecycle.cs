#nullable enable

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ScrollViewer
	{
		private protected override void OnLoaded()
		{
			base.OnLoaded();

			EnsureAttachScrollBars();

			OnLoadedPartial();
		}

		private partial void OnLoadedPartial();

		private protected override void OnUnloaded()
		{
			base.OnUnloaded();

			DetachScrollBars();
			ResetScrollIndicator();

			OnUnloadedPartial();
		}

		private partial void OnUnloadedPartial();
	}
}
