using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Disposables;
using System.Text;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno;
using Uno.Logging;
using Windows.UI.Xaml.Controls;
using Windows.Foundation;
using View = Windows.UI.Xaml.UIElement;
using System.Collections;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml.Media;
using Uno.UI;
using Uno.UI.Xaml;
using Windows.UI;
using System.Dynamic;

namespace Windows.UI.Xaml
{
	public partial class FrameworkElement : IEnumerable
	{
		public new bool IsLoaded => base.IsLoaded; // The IsLoaded state is managed by the UIElement, FrameworkElement only makes it publicly visible

		private protected override sealed void OnElementLoading(int depth)
		{
			base.IsLoading = true;

			OnLoadingPartial();
			ApplyCompiledBindings();

			try
			{
				// Raise event before invoking base in order to raise them top to bottom
				OnLoading();
				_loading?.Invoke(this, new RoutedEventArgs(this));
			}
			catch (Exception error)
			{
				_log.Error("OnElementLoading failed in FrameworkElement", error);
				Application.Current.RaiseRecoverableUnhandledException(error);
			}

			OnPostLoading();

			// Explicit propagation of the loading even must be performed
			// after the compiled bindings are applied (cf. OnLoading), as there may be altered
			// properties that affect the visual tree.
			base.OnElementLoading(depth);
		}

		partial void OnLoadingPartial();
		private protected virtual void OnLoading() { }
		private protected virtual void OnPostLoading() { }

		private protected override sealed void OnElementLoaded()
		{
			if (!base.IsLoaded)
			{
				// Make sure to set the flag before raising the loaded event (duplicated with the base.OnLoaded)
				base.IsLoaded = true;
				base.IsLoading = false;

				OnLoadedPartial();

				try
				{
					// Raise event before invoking base in order to raise them top to bottom
					OnLoaded();
					_loaded?.Invoke(this, new RoutedEventArgs(this));
				}
				catch (Exception error)
				{
					_log.Error("OnElementLoaded failed in FrameworkElement", error);
					Application.Current.RaiseRecoverableUnhandledException(error);
				}
			}

			base.OnElementLoaded();
		}

		partial void OnLoadedPartial();
		private protected virtual void OnLoaded() { }

		private protected override sealed void OnElementUnloaded()
		{
			base.OnElementUnloaded(); // Will set flag IsLoaded to false

			try
			{
				// Raise event after invoking base in order to raise them bottom to top
				OnUnloaded();
				_unloaded?.Invoke(this, new RoutedEventArgs(this));
			}
			catch (Exception error)
			{
				_log.Error("OnElementUnloaded failed in FrameworkElement", error);
				Application.Current.RaiseRecoverableUnhandledException(error);
			}
		}

		private protected virtual void OnUnloaded() { }
	}
}
