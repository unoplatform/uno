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

		private protected sealed override void OnFwEltLoading()
		{
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
		}

		partial void OnLoadingPartial();
		private protected virtual void OnLoading() { }
		private protected virtual void OnPostLoading() { }

		private protected sealed override void OnFwEltLoaded()
		{
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

		partial void OnLoadedPartial();
		private protected virtual void OnLoaded() { }

		private protected sealed override void OnFwEltUnloaded()
		{
			try
			{
				// Raise event after invoking base in order to raise them bottom to top
				OnUnloaded();
				_unloaded?.Invoke(this, new RoutedEventArgs(this));
				OnUnloadedPartial();
			}
			catch (Exception error)
			{
				_log.Error("OnElementUnloaded failed in FrameworkElement", error);
				Application.Current.RaiseRecoverableUnhandledException(error);
			}
		}
		partial void OnUnloadedPartial();

		private protected virtual void OnUnloaded() { }
	}
}
