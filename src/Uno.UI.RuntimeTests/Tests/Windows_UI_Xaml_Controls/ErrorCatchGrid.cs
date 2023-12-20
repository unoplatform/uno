#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class ErrorCatchGrid : Grid
	{
		public Exception? Exception { get; private set; }

		public ErrorCatchGrid()
		{
			Loaded += OnLoaded;
			Unloaded += OnUnloaded;
		}

		private void OnUnloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			Application.Current.UnhandledException -= OnUnhandledException;
		}

		private void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			Application.Current.UnhandledException += OnUnhandledException;
		}

		private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
		{
			Exception ??= e.Exception;
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			try
			{
				return base.MeasureOverride(availableSize);
			}
			catch (Exception e)
			{
				Exception ??= e;
				throw;
			}
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			try
			{
				return base.ArrangeOverride(finalSize);
			}
			catch (Exception e)
			{
				Exception ??= e;
				throw;
			}
		}
	}
}
