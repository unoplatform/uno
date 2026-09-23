using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Disposables;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using MUXControlsTestApp.Utilities;

#if HAS_UNO
using Uno.UI.Dispatching;
using Uno.UI.Xaml.Media;
#endif

namespace Uno.UI.RuntimeTests.Helpers
{
	public static class StyleHelper
	{
		/// <summary>
		/// Adds <paramref name="resources"/> to <see cref="Application.Resources"/> for the duration of the test, then removes it.
		/// </summary>
		public static IDisposable UseAppLevelResources(ResourceDictionary resources)
		{
			var appResources = Application.Current.Resources;
			appResources.MergedDictionaries.Add(resources);

			return Disposable.Create(() =>
			{
				// The runner doesn't unload content between tests, so a view left in the window keeps
				// referencing these entries after they are gone. The next app-wide theme change then
				// re-resolves them, which native WinUI raises as a process-killing unhandled exception.
				TestServices.WindowHelper.WindowContent = null;
				appResources.MergedDictionaries.Remove(resources);
			});
		}

		/// <summary>
		/// Ensure Fluent styles are available for the course of a single test.
		/// </summary>
		public static IDisposable UseUwpStyles()
		{
#if WINAPPSDK // Disabled on WinUI as removing the resource dictionary causes a crash.
			return Disposable.Empty;
#else

			NativeDispatcher.CheckThreadAccess();

			var resources = Application.Current.Resources;
			var xamlResources = resources.MergedDictionaries.OfType<Microsoft.UI.Xaml.Controls.XamlControlsResources>().FirstOrDefault();
			if (xamlResources is null)
			{
				return Disposable.Empty;
			}

			resources.MergedDictionaries.Remove(xamlResources);
			ForceReload();

			IDisposable restore = null;
			restore = Disposable.Create(() =>
			{
				_pendingUwpStylesRestores.Remove(restore);
				resources.MergedDictionaries.Insert(0, xamlResources);
				ForceReload();
			});
			_pendingUwpStylesRestores.Add(restore);

			return restore;

			static void ForceReload()
			{
				DefaultBrushes.ResetDefaultThemeBrushes();
				ResetIslandRootForeground();
			}
#endif
		}

#if !WINAPPSDK
		private static readonly List<IDisposable> _pendingUwpStylesRestores = new();
#endif

		/// <summary>
		/// Restores Fluent styles if a test exited without disposing <see cref="UseUwpStyles"/>,
		/// so the leak does not cascade into every later test of the run.
		/// </summary>
		/// <returns>True if Fluent styles had leaked and were restored.</returns>
		public static bool RestoreLeakedUwpStyles()
		{
#if WINAPPSDK
			return false;
#else
			NativeDispatcher.CheckThreadAccess();

			if (_pendingUwpStylesRestores.Count == 0)
			{
				return false;
			}

			for (var i = _pendingUwpStylesRestores.Count - 1; i >= 0; i--)
			{
				_pendingUwpStylesRestores[i].Dispose();
			}

			return true;
#endif
		}

#if !WINAPPSDK
		private static void ResetIslandRootForeground()
		{
			if (Uno.UI.Xaml.Core.CoreServices.Instance.InitializationType == Xaml.Core.InitializationType.IslandsOnly &&
				VisualTreeUtils.FindVisualChildByType<Control>(TestServices.WindowHelper.XamlRoot.VisualTree.RootElement) is { } control)
			{
				// Ensure the root element's Foreground is set correctly
				control.SetValue(Control.ForegroundProperty, DefaultBrushes.TextForegroundBrush, DependencyPropertyValuePrecedences.DefaultValue);
			}
		}
#endif
	}
}
