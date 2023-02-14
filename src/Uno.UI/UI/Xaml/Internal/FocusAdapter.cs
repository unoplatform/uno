#nullable enable

using Microsoft.UI.Xaml.Input;

namespace Uno.UI.Xaml.Core
{
	// TODO Uno: Add support
	internal class FocusAdapter
	{
		private readonly ContentRoot _contentRoot;

		public FocusAdapter(ContentRoot contentRoot)
		{
			_contentRoot = contentRoot;
		}

		internal virtual void SetFocus() { }

		internal virtual bool ShouldDepartFocus(FocusNavigationDirection direction) => false;
	}
}
