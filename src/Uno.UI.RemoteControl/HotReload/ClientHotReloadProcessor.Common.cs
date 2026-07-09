#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Extensions;
using Uno.UI.Helpers;
using Uno.UI.RemoteControl.HotReload;
using Windows.Storage.Pickers.Provider;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;

#if __APPLE_UIKIT__
using UIKit;
#elif __ANDROID__
using Uno.UI;
#endif

[assembly: System.Reflection.Metadata.MetadataUpdateHandler(typeof(Uno.UI.RemoteControl.HotReload.ClientHotReloadProcessor))]

namespace Uno.UI.RemoteControl.HotReload
{
	partial class ClientHotReloadProcessor
	{
		private static readonly AssemblyLoadContext _processorAlc =
			AssemblyLoadContext.GetLoadContext(typeof(ClientHotReloadProcessor).Assembly)
			?? AssemblyLoadContext.Default;

		private static ClientHotReloadProcessor? _instance;

#if HAS_UNO
		private static ClientHotReloadProcessor? Instance => _instance;
#else
		private static ClientHotReloadProcessor? Instance => _instance ??= new();

		private ClientHotReloadProcessor()
		{
			_status = new(this);
		}
#endif

		// Guards single-arming of the collectible-context Unloading teardown below.
		private static bool _alcTeardownArmed;

		static ClientHotReloadProcessor()
		{
			// Arm at type initialization: in a collectible-context copy the metadata-updater
			// initialization path may never run (e.g. when the dev-server connection is unavailable),
			// yet the per-context statics above still get populated and would pin the context.
			ArmCollectibleAlcTeardown();
		}

		/// <summary>
		/// When this processor copy is owned by a collectible load context (the case where a downstream
		/// host loads previewed apps into their own collectible <see cref="AssemblyLoadContext"/>s, each
		/// loading its own copy of this assembly), arm a teardown that runs when that context unloads.
		/// The per-context static state here — the processor instance, its <c>_agent</c>, and the shared
		/// <c>_elementAgent</c> — each hold a process-wide <see cref="AppDomain.AssemblyLoad"/> subscription
		/// that otherwise keeps the whole context alive after unload. NOTE: <see cref="AssemblyLoadContext.Unloading"/>
		/// was observed NOT to be raised on the browser-wasm runtime, where collectible-context unload is
		/// unimplemented (dotnet/runtime#34072) — there this hook never fires. It is therefore best-effort for
		/// runtimes that do raise the event; a host that needs deterministic release must still dispose the
		/// processor explicitly on teardown (the load-bearing part is that <c>Dispose()</c> on the agents now
		/// clears their Type/delta maps and detaches their <see cref="AppDomain.AssemblyLoad"/> subscriptions).
		/// </summary>
		private static void ArmCollectibleAlcTeardown()
		{
			if (_alcTeardownArmed || _processorAlc == AssemblyLoadContext.Default || !_processorAlc.IsCollectible)
			{
				return;
			}

			_alcTeardownArmed = true;
			_processorAlc.Unloading += static _ => TearDownForAlcUnload();

			if (_log.IsEnabled(LogLevel.Trace))
			{
				_log.Trace("Armed Unloading teardown for the collectible processor load context.");
			}
		}

		/// <summary>
		/// Releases this collectible context's per-context hot-reload state: disposes the processor instance
		/// (which disposes its <c>_agent</c> and detaches that agent's AssemblyLoad subscription), disposes
		/// the shared element-update agent (detaching its AssemblyLoad subscription and clearing its
		/// Type-keyed handler map), and clears the static references. No-op for the default (host) context or
		/// a non-collectible owner, so a live processor is never torn down.
		/// </summary>
		private static void TearDownForAlcUnload()
		{
			if (_processorAlc == AssemblyLoadContext.Default || !_processorAlc.IsCollectible)
			{
				return;
			}

			if (_log.IsEnabled(LogLevel.Trace))
			{
				_log.Trace("Tearing down hot-reload state for the collectible processor load context unload.");
			}

			try
			{
				_instance?.Dispose();
			}
			catch (Exception e)
			{
				_log.Error("Failed to dispose the hot-reload processor during collectible context teardown.", e);
			}

			_instance = null;

			try
			{
				_elementAgent?.Dispose();
			}
			catch (Exception e)
			{
				_log.Error("Failed to dispose the element-update agent during collectible context teardown.", e);
			}

			_elementAgent = null;
			CurrentWindow = null;
		}

		private static async IAsyncEnumerable<TMatch> EnumerateHotReloadInstances<TMatch>(
			object? instance,
			Func<FrameworkElement, string, Task<TMatch?>> predicate,
			string? parentKey)
		{

			if (instance is FrameworkElement fe)
			{
				var instanceTypeName = GetInstanceTypeName(instance);
				var instanceKey = parentKey is not null ? $"{parentKey}_{instanceTypeName}" : instanceTypeName;
				var match = await predicate(fe, instanceKey);
				if (match is not null)
				{
					yield return match;
				}

				// Stop at AlcContentHost boundaries — the inner app's own processor
				// will handle its subtree starting from window.Content (which resolves
				// to the AlcContentHost content via TryGetContentFromSecondaryAlc).
				var skipChildren = false;
#if HAS_UNO
				if (fe is Uno.UI.Xaml.Controls.AlcContentHost)
				{
					if (_log.IsEnabled(LogLevel.Information))
					{
						_log.Info("[HotReload] AlcContentHost encountered — skipping children (handled by inner ALC processor)");
					}

					skipChildren = true;
				}
#endif
				if (!skipChildren)
				{
					var idx = 0;
					foreach (var child in fe.EnumerateChildren())
					{
						var inner = EnumerateHotReloadInstances(child, predicate, $"{instanceKey}_[{idx}]");
						idx++;
						await foreach (var validElement in inner)
						{
							yield return validElement;
						}
					}
				}
			}
#if __IOS__ || __ANDROID__
#if __IOS__
			else if (instance is UIView nativeView)
#elif __ANDROID__
			else if (instance is global::Android.Views.ViewGroup nativeView)
#endif
			{
				// Enumerate through native instances, such as NativeFramePresenter

				var idx = 0;
				foreach (var nativeChild in nativeView.EnumerateChildren())
				{
					var instanceTypeName = GetInstanceTypeName(instance);
					var instanceKey = parentKey is not null ? $"{parentKey}_{instanceTypeName}" : instanceTypeName;
					var inner = EnumerateHotReloadInstances(nativeChild, predicate, $"{instanceKey}_[{idx}]");

					idx++;

					await foreach (var validElement in inner)
					{
						yield return validElement;
					}
				}
			}
#endif
		}

		[UnconditionalSuppressMessage("Trimming", "IL2072")]
		private static string GetInstanceTypeName(object value)
			=> (value.GetType().GetOriginalType() ?? value.GetType()).Name;

		private static void SwapViews(FrameworkElement oldView, FrameworkElement newView)
		{
			if (_log.IsEnabled(LogLevel.Trace))
			{
				_log.Trace($"Swapping view {newView.GetType()}");
			}

#if !WINUI
			var parentAsContentControl = oldView.GetVisualTreeParent() as ContentControl;
			parentAsContentControl = parentAsContentControl ?? (oldView.GetVisualTreeParent() as ContentPresenter)?.FindFirstParent<ContentControl>();
#else
			var parentAsContentControl = VisualTreeHelper.GetParent(oldView) as ContentControl;
			parentAsContentControl = parentAsContentControl ?? (VisualTreeHelper.GetParent(oldView) as ContentPresenter)?.FindFirstParent<ContentControl>();
#endif

#if !HAS_UNO
			var parentDataContext = (parentAsContentControl as FrameworkElement)?.DataContext;
			var oldDataContext = oldView.DataContext;
#endif
			if ((parentAsContentControl?.Content as FrameworkElement) == oldView)
			{
				parentAsContentControl.Content = newView;
			}
			else if (newView is Page newPage && oldView is Page oldPage)
			{
				// In the case of Page, swapping the actual page is not supported, so we
				// need to swap the content of the page instead. This can happen if the Frame
				// is using a native presenter which does not use the `Frame.Content` property.
				oldPage.Content = newPage;
#if !WINUI
				newPage.Frame = oldPage.Frame;
#endif
			}
#if !WINUI
			// Currently we don't have SwapViews implementation that works with WinUI
			// so skip swapping non-Page views initially for WinUI
			else
			{
				VisualTreeHelper.SwapViews(oldView, newView);
			}
#endif

#if !HAS_UNO
			if (oldView is FrameworkElement oldViewAsFE && newView is FrameworkElement newViewAsFE)
			{
				ApplyDataContext(parentDataContext, oldViewAsFE, newViewAsFE, oldDataContext);
			}
#endif
		}

#if !HAS_UNO
		private static void ApplyDataContext(
			object? parentDataContext,
			FrameworkElement oldView,
			FrameworkElement newView,
			object? oldDataContext)
		{
			if (oldView == null || newView == null)
			{
				return;
			}

			if ((newView.DataContext is null || newView.DataContext == parentDataContext)
				&& (oldDataContext is not null && oldDataContext != parentDataContext))
			{
				// If the DataContext is not provided by the page itself, it may
				// have been provided by an external actor. Copy the value as is
				// in the DataContext of the new element.

				newView.DataContext = oldDataContext;
			}
		}
#endif
	}
}
