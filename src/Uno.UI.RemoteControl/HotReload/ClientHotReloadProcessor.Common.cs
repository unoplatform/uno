#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Extensions;
using Uno.UI.Helpers;
using Uno.UI.RemoteControl.HotReload;
using Windows.Storage.Pickers.Provider;


#if __APPLE_UIKIT__
using UIKit;
#elif __ANDROID__
using Uno.UI;
#endif

#if __APPLE_UIKIT__
using UIKit;
using _View = UIKit.UIView;
#elif __ANDROID__
using _View = Android.Views.View;
#else
using _View = Microsoft.UI.Xaml.DependencyObject;
#endif

[assembly: System.Reflection.Metadata.MetadataUpdateHandler(typeof(Uno.UI.RemoteControl.HotReload.ClientHotReloadProcessor))]

namespace Uno.UI.RemoteControl.HotReload
{
	partial class ClientHotReloadProcessor
	{
		private static IEnumerable<(FrameworkElement element, string key)> EnumerateVisualTree(object? instance, string parentKey)
		{
			if (instance is not _View view)
			{
				yield break;
			}

			var instanceTypeName = (instance.GetType().GetOriginalType() ?? instance.GetType()).Name;
			var instanceKey = $"{parentKey}_{instanceTypeName}";

			if (view is FrameworkElement element)
			{
				yield return (element, instanceKey);
			}

			for (var i = 0; i < VisualTreeHelper.GetChildrenCount(view); i++)
			{
				var child = VisualTreeHelper.GetChild(view, i);
				var inner = EnumerateVisualTree(child, $"{instanceKey}_[{i}]");

				foreach (var childElement in inner)
				{
					yield return childElement;
				}
			}
		}

		private static void IterateVisualTree(object? instance, Func<FrameworkElement, bool> action)
		{
			if (instance is not _View view)
			{
				return;
			}

			if (view is FrameworkElement element)
			{
				var stop = action(element);
				if (stop)
				{
					return;
				}
			}

			for (var i = 0; i < VisualTreeHelper.GetChildrenCount(view); i++)
			{
				IterateVisualTree(VisualTreeHelper.GetChild(view, i), action);
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
