using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Extensions;
using Uno.UI.Helpers;
using Uno.UI.RemoteControl.HotReload;
using Uno.UI.RemoteControl.HotReload.Messages;
using Windows.Storage.Pickers.Provider;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
#if __IOS__
using _View = UIKit.UIView;
#else
using _View = Windows.UI.Xaml.FrameworkElement;
#endif

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
		private static async IAsyncEnumerable<TMatch> EnumerateHotReloadInstances<TMatch>(
			object instance,
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

		private static void SwapViews(_View oldView, _View newView)
		{
			if (_log.IsEnabled(LogLevel.Trace))
			{
				_log.Trace($"Swapping view {newView.GetType()}");
			}

			var parentAsContentControl = oldView.GetVisualTreeParent() as ContentControl;
			parentAsContentControl = parentAsContentControl ?? (oldView.GetVisualTreeParent() as ContentPresenter)?.FindFirstParent<ContentControl>();

			if (parentAsContentControl?.Content == oldView)
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
				oldPage.ClearValue(Page.DataContextProperty, DependencyPropertyValuePrecedences.Local);

				oldPage.Content = newPage;
				newPage.Frame = oldPage.Frame;
			}
			else
			{
				VisualTreeHelper.SwapViews(oldView, newView);
			}

			if (oldView is FrameworkElement oldViewAsFE && newView is FrameworkElement newViewAsFE)
			{
				PropagateProperties(oldViewAsFE, newViewAsFE);
			}
		}

		private static void PropagateProperties(FrameworkElement oldView, FrameworkElement newView)
		{
			if (oldView == null || newView == null)
			{
				return;
			}

			if (newView.DataContext is null
				&& oldView.DataContext is not null)
			{
				// If the DataContext is not provided by the page itself, it may
				// have been provided by an external actor. Copy the value as is
				// in the DataContext of the new element.

				newView.DataContext = oldView.DataContext;
			}
		}
	}
}
