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
		/// Enables styles associated with native frame navigation (on Android and iOS) for the duration of the test.
		/// </summary>
		public static IDisposable UseNativeFrameNavigation()
		{
#if WINAPPSDK
			return null;
#else
			return new CompositeDisposable
			{
				UseNativeStyle<Frame>(),
				UseNativeStyle<CommandBar>(),
				UseNativeStyle<AppBarButton>(),
			};
#endif
		}

		/// <summary>
		/// Enables the native style for <typeparamref name="T"/> for the duration of the test, if one is available.
		/// </summary>
		public static IDisposable UseNativeStyle<T>() where T : Control
		{
#if WINAPPSDK
			return null;
#else
			IDisposable disposable;
			if (FeatureConfiguration.Style.UseUWPDefaultStylesOverride.TryGetValue(typeof(T), out var currentOverride))
			{
				disposable = Disposable.Create(() => FeatureConfiguration.Style.UseUWPDefaultStylesOverride[typeof(T)] = currentOverride);
			}
			else
			{
				disposable = Disposable.Create(() => FeatureConfiguration.Style.UseUWPDefaultStylesOverride.Remove(typeof(T)));
			}

			FeatureConfiguration.Style.UseUWPDefaultStylesOverride[typeof(T)] = false;

			return disposable;
#endif
		}

		/// <summary>
		/// Adds <paramref name="resources"/> to <see cref="Application.Resources"/> for the duration of the test, then removes it.
		/// </summary>
		public static IDisposable UseAppLevelResources(ResourceDictionary resources)
		{
			var appResources = Application.Current.Resources;
			appResources.MergedDictionaries.Add(resources);

			return Disposable.Create(() => appResources.MergedDictionaries.Remove(resources));
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
			var xamlResources = resources.MergedDictionaries.OfType<Microsoft/* UWP don't rename */.UI.Xaml.Controls.XamlControlsResources>().FirstOrDefault();
			if (xamlResources is null)
			{
				return Disposable.Empty;
			}

			resources.MergedDictionaries.Remove(xamlResources);
			ForceReload();

			return new DisposableAction(() =>
			{
				resources.MergedDictionaries.Insert(0, xamlResources);
				ForceReload();
			});

			static void ForceReload()
			{
				DefaultBrushes.ResetDefaultThemeBrushes();
				ResetIslandRootForeground();
			}
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
