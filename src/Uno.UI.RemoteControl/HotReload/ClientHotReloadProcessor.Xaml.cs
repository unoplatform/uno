using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.RemoteControl.HotReload.Messages;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;

namespace Uno.UI.RemoteControl.HotReload
{
	partial class ClientHotReloadProcessor
	{
		private async Task ReloadFile(FileReload fileReload)
		{
			Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(
				Windows.UI.Core.CoreDispatcherPriority.Normal,
				async () =>
            {
                try
                {
                    if (this.Log().IsEnabled(LogLevel.Debug))
                    {
                        this.Log().LogDebug($"Reloading changed file [{fileReload.FilePath}]");
                    }

                    var uri = new Uri("file:///" + fileReload.FilePath.Replace("\\", "/"));

					Application.RegisterComponent(uri, fileReload.Content);

					foreach (var instance in EnumerateInstances(Window.Current.Content, uri))
                    {
                        switch (instance)
                        {
#if __IOS__
						case UserControl userControl:
							userControl.Content = XamlReader.Load(fileReload.Content) as UIKit.UIView;
							break;
#endif
                            case ContentControl content:
                                content.Content = XamlReader.LoadUsingXClass(fileReload.Content);
                                break;
                        }
					}

					if (ResourceResolver.RetrieveDictionaryForFilePath(uri.AbsolutePath) is { } targetDictionary)
					{
						var replacementDictionary = (ResourceDictionary)XamlReader.Load(fileReload.Content);
						targetDictionary.CopyFrom(replacementDictionary);
						Application.Current.UpdateResourceBindingsForHotReload();
					}
				}
                catch (Exception e)
                {
                    if (this.Log().IsEnabled(LogLevel.Error))
                    {
                        this.Log().LogError($"Failed reloading changed file [{fileReload.FilePath}]", e);
                    }

					await _rcClient.SendMessage(
						new HotReload.Messages.XamlLoadError(
							filePath: fileReload.FilePath,
							exceptionType: e.GetType().ToString(),
							message: e.Message,
							stackTrace: e.StackTrace));
				}
			});
		}

		private IEnumerable<UIElement> EnumerateInstances(object instance, Uri baseUri)
		{
			if (
				instance is FrameworkElement fe && baseUri.OriginalString == fe.BaseUri?.OriginalString)
			{
				yield return fe;
			}
			else if(instance != null)
			{
				IEnumerable<IEnumerable<UIElement>> Dig()
				{
					switch (instance)
					{
						case Panel panel:
							foreach (var child in panel.Children)
							{
								yield return EnumerateInstances(child, baseUri);
							}
							break;

						case Border border:
							yield return EnumerateInstances(border.Child, baseUri);
							break;

						case ContentControl control when control.ContentTemplateRoot != null || control.Content != null: 
							yield return EnumerateInstances(control.ContentTemplateRoot ?? control.Content, baseUri);
							break;

						case Control control:
							yield return EnumerateInstances(control.TemplatedRoot, baseUri);
							break;

						case ContentPresenter presenter:
							yield return EnumerateInstances(presenter.Content, baseUri);
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
	}
}
