using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Uno.Extensions;
using Uno.Foundation.Logging;
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
		private static IEnumerable<TMatch> EnumerateHotReloadInstances<TMatch>(
			object instance,
			Func<FrameworkElement, TMatch?> predicate,
			bool enumerateChildrenAfterMatch = false)
		{
			if (instance is FrameworkElement fe)
			{
				var match = predicate(fe);
				if (match is not null)
				{
					yield return match;

					// If we found a match, we don't need to enumerate the children
					if (!enumerateChildrenAfterMatch)
					{
						yield break;
					}
				}

				IEnumerable<IEnumerable<TMatch>> Dig()
				{
					switch (instance)
					{
						case Panel panel:
							foreach (var child in panel.Children)
							{
								yield return EnumerateHotReloadInstances(child, predicate, enumerateChildrenAfterMatch);
							}
							break;

						case Border border:
							yield return EnumerateHotReloadInstances(border.Child, predicate, enumerateChildrenAfterMatch);
							break;

						case ContentControl control when control.ContentTemplateRoot != null || control.Content != null:
							yield return EnumerateHotReloadInstances(control.ContentTemplateRoot ?? control.Content, predicate, enumerateChildrenAfterMatch);
							break;

						case Control control:
							yield return EnumerateHotReloadInstances(control.TemplatedRoot, predicate, enumerateChildrenAfterMatch);
							break;

						case ContentPresenter presenter:
							yield return EnumerateHotReloadInstances(presenter.Content, predicate, enumerateChildrenAfterMatch);
							break;
					}
				}

				foreach (var inner in Dig())
				{
					foreach (var validElement in inner)
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

			var commonProperties = from oldProp in DependencyProperty.GetPropertiesForType(oldView.GetType())
								   from newProp in DependencyProperty.GetPropertiesForType(newView.GetType())
								   where oldProp.Name == newProp.Name
								   let oldValue = oldView.GetValue(oldProp, DependencyPropertyValuePrecedences.Local)
								   let newValue = newView.GetValue(newProp)
								   where oldValue is not null && newValue is null
								   select (oldProp, oldValue, newProp);
			foreach (var p in commonProperties)
			{
				newView.SetValue(p.newProp, p.oldValue);
			}


		}
	}
}
