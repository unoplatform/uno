using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Disposables;
using System.Text;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno;
using Uno.Foundation.Logging;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using View = Microsoft.UI.Xaml.UIElement;
using System.Collections;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Media;
using Uno.UI;
using Uno.UI.Xaml;
using Windows.UI;
using System.Dynamic;

namespace Microsoft.UI.Xaml
{
	public partial class FrameworkElement : IEnumerable
	{
		public new bool IsLoaded => base.IsLoaded; // The IsLoaded state is managed by the UIElement, FrameworkElement only makes it publicly visible

		private protected sealed override void OnFwEltLoading()
		{
			OnLoadingPartial();

			void InvokeLoading()
			{
				// Raise event before invoking base in order to raise them top to bottom
				OnLoading();
				_loading?.Invoke(this, new RoutedEventArgs(this));
			}

			if (FeatureConfiguration.FrameworkElement.HandleLoadUnloadExceptions)
			{
				/// <remarks>
				/// This method contains or is called by a try/catch containing method and
				/// can be significantly slower than other methods as a result on WebAssembly.
				/// See https://github.com/dotnet/runtime/issues/56309
				/// </remarks>
				void InvokeLoadingWithTry()
				{
					try
					{
						InvokeLoading();
					}
					catch (Exception error)
					{
						_log.Error("OnElementLoading failed in FrameworkElement", error);
						Application.Current.RaiseRecoverableUnhandledException(error);
					}
				}

				InvokeLoadingWithTry();
			}
			else
			{
				InvokeLoading();
			}


			OnPostLoading();
		}

		partial void OnLoadingPartial();
		private protected virtual void OnLoading() { }
		private protected virtual void OnPostLoading() { }

		private protected sealed override void OnFwEltLoaded()
		{
			OnLoadedPartial();

			void InvokeLoaded()
			{
				// Raise event before invoking base in order to raise them top to bottom
				OnLoaded();
				_loaded?.Invoke(this, new RoutedEventArgs(this));
			}

			if (FeatureConfiguration.FrameworkElement.HandleLoadUnloadExceptions)
			{
				/// <remarks>
				/// This method contains or is called by a try/catch containing method and
				/// can be significantly slower than other methods as a result on WebAssembly.
				/// See https://github.com/dotnet/runtime/issues/56309
				/// </remarks>
				void InvokeLoadedWithTry()
				{
					try
					{
						InvokeLoaded();
					}
					catch (Exception error)
					{
						_log.Error("OnElementLoaded failed in FrameworkElement", error);
						Application.Current.RaiseRecoverableUnhandledException(error);
					}
				}

				InvokeLoadedWithTry();
			}
			else
			{
				InvokeLoaded();
			}
		}

		partial void OnLoadedPartial();
		private protected virtual void OnLoaded()
		{
			ReconfigureViewportPropagationPartial();
		}

		private partial void ReconfigureViewportPropagationPartial();

		private protected sealed override void OnFwEltUnloaded()
		{
			void InvokeUnloaded()
			{
				// Raise event after invoking base in order to raise them bottom to top
				OnUnloaded();
				_unloaded?.Invoke(this, new RoutedEventArgs(this));
				OnUnloadedPartial();
			}

			if (FeatureConfiguration.FrameworkElement.HandleLoadUnloadExceptions)
			{
				/// <remarks>
				/// This method contains or is called by a try/catch containing method and
				/// can be significantly slower than other methods as a result on WebAssembly.
				/// See https://github.com/dotnet/runtime/issues/56309
				/// </remarks>
				void InvokeUnloadedWithTry()
				{
					try
					{
						InvokeUnloaded();
					}
					catch (Exception error)
					{
						_log.Error("OnElementUnloaded failed in FrameworkElement", error);
						Application.Current.RaiseRecoverableUnhandledException(error);
					}
				}

				InvokeUnloadedWithTry();
			}
			else
			{
				InvokeUnloaded();
			}
		}

		partial void OnUnloadedPartial();

		private protected virtual void OnUnloaded()
		{
			ReconfigureViewportPropagationPartial();
		}

		public override string ToString()
		{
#if __WASM__
			if (FeatureConfiguration.UIElement.RenderToStringWithId && !Name.IsNullOrEmpty())
			{
				return $"{base.ToString()}\"{Name}\"";
			}
#endif

			return base.ToString();
		}
	}
}
