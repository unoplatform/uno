using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using Uno.Disposables;
using Uno.Logging;
using Uno.UI;
using Windows.UI.Xaml.Input;

namespace Windows.UI.Xaml.Controls
{
	public partial class ToggleSwitch
	{
		protected override void OnLoaded()
		{
			base.OnLoaded();

			PointerExited += OnPointerExited;
			PointerEntered += OnPointerEntered;
			PointerCanceled += OnPointerCanceled;
			PointerPressed += OnPointerPressed;
			PointerReleased += OnPointerReleased;
		}

		private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
		{
			OnPointerReleased(e);
		}

		private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
		{
			OnPointerPressed(e);
		}

		private void OnPointerCanceled(object sender, PointerRoutedEventArgs e)
		{
			OnPointerCanceled(e);
		}

		private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
		{
			OnPointerEntered(e);
		}

		private void OnPointerExited(object sender, PointerRoutedEventArgs e)
		{
			OnPointerExited(e);
		}

		protected override void OnUnloaded()
		{
			base.OnUnloaded();

			PointerExited -= OnPointerExited;
			PointerEntered -= OnPointerEntered;
			PointerCanceled -= OnPointerCanceled;
			PointerPressed -= OnPointerPressed;
			PointerReleased -= OnPointerReleased;
		}
	}
}
