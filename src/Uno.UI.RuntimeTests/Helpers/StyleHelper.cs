using System;
using System.Collections.Generic;
using System.Text;
using Uno.Disposables;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Helpers
{
	public static class StyleHelper
	{
		/// <summary>
		/// Enables styles associated with native frame navigation (on Android and iOS) for the duration of the test.
		/// </summary>
		public static IDisposable UseNativeFrameNavigation()
		{
#if NETFX_CORE
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
#if NETFX_CORE
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
	}
}
