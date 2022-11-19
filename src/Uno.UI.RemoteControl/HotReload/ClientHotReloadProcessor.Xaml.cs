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
        private static Logger _log = typeof(ClientHotReloadProcessor).Log();
        private string? _lastUpdatedFilePath;

		private void ReloadFile(FileReload fileReload)
		{
			if (string.Equals(Environment.GetEnvironmentVariable("DOTNET_MODIFIABLE_ASSEMBLIES"), "debug", StringComparison.OrdinalIgnoreCase))
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().LogDebug($".NET Hot Reload is enabled, skipping XAML Reader reload");
				}
				return;
			}

            if (!fileReload.IsValid())
            {
                if (fileReload.FilePath.HasValue() && this.Log().IsEnabled(LogLevel.Debug))
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

                var uri = new Uri("file:///" + filePath.Replace("\\", "/"));

                Application.RegisterComponent(uri, fileContent);

                foreach (var instance in EnumerateInstances(Window.Current.Content, i => uri.OriginalString == i.BaseUri?.OriginalString))
                {
                    switch (instance)
                    {
#if __IOS__
						case UserControl userControl:
							SwapViews(userControl, XamlReader.LoadUsingXClass(fileReload.Content) as UIKit.UIView);
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
			if (
				instance is FrameworkElement fe && predicate(fe))
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

			PropagateProperties(oldView as FrameworkElement, newView as FrameworkElement);
		}

		private static void PropagateProperties(FrameworkElement oldView, FrameworkElement newView)
		{
			if (oldView == null || newView == null)
			{
				return;
			}
			newView.BaseUri = oldView.BaseUri;

			if (oldView is Page oldPage && newView is Page newPage)
			{
				newPage.Frame = oldPage.Frame;

				// If we've replaced the Page in its frame, we may need to
				// swap the content property as well. If may be required
				// if the frame is handled by a (native) FramePresenter.
				newPage.Frame.Content = newPage;
			}

			if(newView.DataContext is null
				&& oldView.DataContext is not null)
			{
				// If the DataContext is not provided by the page itself, it may
				// have been provided by an external actor. Copy the value as is
				// in the DataContext of the new element.

				newView.DataContext = oldView.DataContext;
			}
		}

        private static void ReloadWithUpdatedTypes(Type[] updatedTypes)
        {
            if(updatedTypes.Length == 0)
            {
                ReloadWithLastChangedFile();
                return;
            }

            foreach (var updatedType in updatedTypes)
            {
                if (_log.IsEnabled(LogLevel.Debug))
                {
                    _log.LogDebug($"Processing changed type [{updatedType}]");
                }

                if (updatedType.Is<UIElement>())
                {
                    ReplaceViewInstances(i => updatedType.IsInstanceOfType(i));
                }
                else
                {
                    if (_log.IsEnabled(LogLevel.Debug))
                    {
                        _log.LogDebug($"Type [{updatedType}] is not a UIElement, skipping");
                    }
                }
            }
        }

        /// <summary>
        /// Reload with the last updated file after metadata was updated
        /// </summary>
        /// <remarks>
        /// This scenario can happen when using WebAssembly from VisualStudio 2022, where changed types are not provided by browserlink.
        /// </remarks>
        private static void ReloadWithLastChangedFile()
        {
            var lastUpdated = _instance?._lastUpdatedFilePath;

            if (lastUpdated is null)
            {
                if (_log.IsEnabled(LogLevel.Debug))
                {
                    _log.LogDebug($"Last changed filed is not available, skipping");
                }

                return;
            }

            if (_log.IsEnabled(LogLevel.Debug))
            {
                _log.LogDebug($"Processing last changed file [{lastUpdated}]");
            }

            var uri = new Uri("file:///" + lastUpdated.Replace("\\", "/"));

            // Search for all types in the main window's tree that
            // match the last modified uri.
            ReplaceViewInstances(i => uri.OriginalString == i.BaseUri?.OriginalString);
        }

        private static void ReplaceViewInstances(Func<FrameworkElement, bool> predicate)
        {
            foreach (var instance in EnumerateInstances(Window.Current.Content, predicate))
            {
                if (instance.GetType().GetConstructor(Array.Empty<Type>()) is { })
                {
                    if (_log.IsEnabled(LogLevel.Trace))
                    {
                        _log.Trace($"Creating instance of type {instance.GetType()}");
                    }

                    var newInstance = Activator.CreateInstance(instance.GetType());

                    switch (instance)
                    {
#if __IOS__
						case UserControl userControl:
							SwapViews(userControl, newInstance as UIKit.UIView);
							break;
#endif
                        case ContentControl content:
                            if (newInstance is ContentControl newContent)
                            {
                                SwapViews(content, newContent);
                            }
                            break;
                    }
                }
                else
                {
                    if (_log.IsEnabled(LogLevel.Debug))
                    {
                        _log.LogDebug($"Type [{instance.GetType()}] has no parameterless constructor, skipping reload");
                    }
                }
            }
        }

        public static void UpdateApplication(Type[] types)
		{
			if (_log.IsEnabled(LogLevel.Trace))
			{
				_log.Trace($"UpdateApplication (changed types: {string.Join(", ", types.Select(s => s.ToString()))})");
			}

            _ = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(
                Windows.UI.Core.CoreDispatcherPriority.Normal,
                () => ReloadWithUpdatedTypes(types));
		}
	}
}
