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
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
#if __IOS__
using _View = UIKit.UIView;
#else
using _View = Microsoft.UI.Xaml.FrameworkElement;
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
		private static Logger _log = typeof(ClientHotReloadProcessor).Log();
		private string? _lastUpdatedFilePath;

		private void ReloadFile(FileReload fileReload)
		{
			if (string.Equals(Environment.GetEnvironmentVariable("DOTNET_MODIFIABLE_ASSEMBLIES"), "debug", StringComparison.OrdinalIgnoreCase)
				&& !_useXamlReaderHotReload)
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().LogDebug($".NET Hot Reload is enabled, skipping XAML Reader reload");
				}
				return;
			}

			if (!fileReload.IsValid())
			{
				if (fileReload.FilePath.IsNullOrEmpty() && this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().LogDebug($"FileReload is missing a file path");
				}

				if (fileReload.Content is null && this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().LogDebug($"FileReload is missing content");
				}

				return;
			}

			_lastUpdatedFilePath = fileReload.FilePath;

			_ = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(
				Windows.UI.Core.CoreDispatcherPriority.Normal,
				async () =>
				{
					await ReloadWithFileAndContent(fileReload.FilePath, fileReload.Content);
				});
		}

		private async Task ReloadWithFileAndContent(string filePath, string fileContent)
		{
			try
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().LogDebug($"Reloading changed file [{filePath}]");
				}

				var uri = new Uri("file:///" + filePath.Replace('\\', '/'));

				Application.RegisterComponent(uri, fileContent);

				bool IsSameBaseUri(FrameworkElement i)
				{
					return uri.OriginalString == i.DebugParseContext?.LocalFileUri

						// Compatibility with older versions of Uno, where BaseUri is set to the
						// local file path instead of the component Uri.
						|| uri.OriginalString == i.BaseUri?.OriginalString;
				}

				foreach (var instance in EnumerateInstances(Window.Current.Content, IsSameBaseUri))
				{
					switch (instance)
					{
#if __IOS__
						case UserControl userControl:
							if (XamlReader.LoadUsingXClass(fileContent) is UIKit.UIView newInstance)
							{
								SwapViews(userControl, newInstance);
							}
							break;
#endif
						case ContentControl content:
							if (XamlReader.LoadUsingXClass(fileContent) is ContentControl newContent)
							{
								SwapViews(content, newContent);
							}
							break;
					}
				}

				if (ResourceResolver.RetrieveDictionaryForFilePath(uri.AbsolutePath) is { } targetDictionary)
				{
					var replacementDictionary = (ResourceDictionary)XamlReader.Load(fileContent);
					targetDictionary.CopyFrom(replacementDictionary);
					Application.Current.UpdateResourceBindingsForHotReload();
				}
			}
			catch (Exception e)
			{
				if (e is TargetInvocationException { InnerException: { } innerException })
				{
					e = innerException;
				}

				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().LogError($"Failed reloading changed file [{filePath}]", e);
				}

				await _rcClient.SendMessage(
					new HotReload.Messages.XamlLoadError(
						filePath: filePath,
						exceptionType: e.GetType().ToString(),
						message: e.Message,
						stackTrace: e.StackTrace));
			}
		}

		private static IEnumerable<UIElement> EnumerateInstances(object instance, Func<FrameworkElement, bool> predicate)
		{
			if (instance is FrameworkElement fe && predicate(fe))
			{
				yield return fe;
			}
			else if (instance != null)
			{
				IEnumerable<IEnumerable<UIElement>> Dig()
				{
					switch (instance)
					{
						case Panel panel:
							foreach (var child in panel.Children)
							{
								yield return EnumerateInstances(child, predicate);
							}
							break;

						case Border border:
							yield return EnumerateInstances(border.Child, predicate);
							break;

						case ContentControl control when control.ContentTemplateRoot != null || control.Content != null:
							yield return EnumerateInstances(control.ContentTemplateRoot ?? control.Content, predicate);
							break;

						case Control control:
							yield return EnumerateInstances(control.TemplatedRoot, predicate);
							break;

						case ContentPresenter presenter:
							yield return EnumerateInstances(presenter.Content, predicate);
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

			newView.SetBaseUri(
				oldView.BaseUri.OriginalString,
				oldView.DebugParseContext?.LocalFileUri ?? "",
				oldView.DebugParseContext?.LineNumber ?? -1,
				oldView.DebugParseContext?.LinePosition ?? -1);

			if (oldView is Page oldPage && newView is Page newPage)
			{
				newPage.Frame = oldPage.Frame;

				// If we've replaced the Page in its frame, we may need to
				// swap the content property as well. If may be required
				// if the frame is handled by a (native) FramePresenter.
				newPage.Frame.Content = newPage;
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
