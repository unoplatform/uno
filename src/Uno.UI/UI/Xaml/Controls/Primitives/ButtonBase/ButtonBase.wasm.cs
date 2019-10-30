using Uno;
using Uno.Client;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI;
using Windows.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Disposables;
using System.Text;
using System.Threading;
using Windows.System;
using Uno.Foundation;

using View = Windows.UI.Xaml.UIElement;

namespace Windows.UI.Xaml.Controls.Primitives
{
	public partial class ButtonBase : ContentControl
	{
		partial void PartialInitializeProperties()
		{
			// We need to ensure the "Tapped" event is registered
			// for the "Click" event to work properly
			Tapped += (snd, evt) => { };
		}

		protected override void OnLoaded()
		{
			base.OnLoaded();

			RegisterEvents();

			KeyDown += OnKeyDown;
		}

		protected override void OnUnloaded()
		{
			base.OnUnloaded();

			KeyDown -= OnKeyDown;
		}

		private void OnKeyDown(object sender, KeyRoutedEventArgs keyRoutedEventArgs)
		{
			switch (keyRoutedEventArgs?.Key)
			{
				case VirtualKey.Enter:
				case VirtualKey.Execute:
				case VirtualKey.Space:
					OnClick();
					break;
			}
		}

		/// <summary>
		/// Gets the native UI Control, if any.
		/// </summary>
		private View GetUIControl()
		{
			return
				// Check for non-templated ContentControl root (ContentPresenter bypass)
				ContentTemplateRoot

				// Finally check for templated ContentControl root
				?? TemplatedRoot;
		}		
	}
}
