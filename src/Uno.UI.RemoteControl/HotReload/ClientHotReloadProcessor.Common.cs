#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Extensions;
using Uno.UI.Helpers;
using Uno.UI.RemoteControl.HotReload;
using Windows.Storage.Pickers.Provider;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;

#if __IOS__
using UIKit;
#elif __MACOS__
using AppKit;
#elif __ANDROID__
using Uno.UI;
#endif

[assembly: System.Reflection.Metadata.MetadataUpdateHandler(typeof(Uno.UI.RemoteControl.HotReload.ClientHotReloadProcessor))]

namespace Uno.UI.RemoteControl.HotReload
{
	partial class ClientHotReloadProcessor
	{
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

		private static async IAsyncEnumerable<TMatch> EnumerateHotReloadInstances<TMatch>(
			object? instance,
			Func<FrameworkElement, string, Task<TMatch?>> predicate,
			string? parentKey)
		{

			if (instance is FrameworkElement fe)
			{
				var instanceTypeName = (instance.GetType().GetOriginalType() ?? instance.GetType()).Name;
				var instanceKey = parentKey is not null ? $"{parentKey}_{instanceTypeName}" : instanceTypeName;
				var match = await predicate(fe, instanceKey);
				if (match is not null)
				{
					yield return match;
				}

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

				// Clear any local context, so that the new page can inherit the value coming
				// from the parent Frame. It may happen if the old page set it explicitly.

#if !WINUI
				oldPage.ClearValue(Page.DataContextProperty, DependencyPropertyValuePrecedences.Local);
#else
				oldPage.ClearValue(Page.DataContextProperty);
#endif

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
