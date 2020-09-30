#nullable enable

using Windows.UI.Xaml;

namespace Uno.UI.Xaml.Input
{
	internal struct ChangingFocusEventRaiseResult
	{
		public ChangingFocusEventRaiseResult(bool canceled, DependencyObject? finalGettingFocusElement = null)
		{
			Canceled = canceled;
			FinalGettingFocusElement = finalGettingFocusElement;
		}

		public bool Canceled { get; }

		public DependencyObject? FinalGettingFocusElement { get; }
	}
}
