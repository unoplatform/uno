using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Uno.Extensions;
using Uno.UI.RemoteControl.HotReload.Messages;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;

namespace Uno.UI.RemoteControl.HotReload
{
	public class ClientHotReloadProcessor : IRemoteControlProcessor
	{
		private string _projectPath;
		private string[] _xamlPaths;

		private readonly IRemoteControlClient _rcClient;

		public ClientHotReloadProcessor(IRemoteControlClient rcClient)
		{
			_rcClient = rcClient;
		}

		string IRemoteControlProcessor.Scope => HotReloadConstants.ScopeName;

		public async Task Initialize()
		{
			await ConfigureServer();
		}

		public async Task ProcessFrame(Messages.Frame frame)
		{
			switch (frame.Name)
			{
				case FileReload.Name:
					ReloadFile(JsonConvert.DeserializeObject<HotReload.Messages.FileReload>(frame.Content));
					break;
			}
		}

		private async Task ConfigureServer()
		{
			if (_rcClient.AppType.Assembly.GetCustomAttributes(typeof(ProjectConfigurationAttribute), false) is ProjectConfigurationAttribute[] configs)
			{
				var config = configs.First();

				_projectPath = config.ProjectPath;
				_xamlPaths = config.XamlPaths;

				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().LogDebug($"ProjectConfigurationAttribute={config.ProjectPath}, Paths={_xamlPaths.Length}");
				}
			}
			else
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().LogError("Unable to find ProjectConfigurationAttribute");
				}
			}

			await _rcClient.SendMessage(new HotReload.Messages.ConfigureServer(_projectPath, _xamlPaths));
		}

		private async Task ReloadFile(FileReload fileReload)
		{
			Windows.UI.Core.CoreDispatcher.Main.RunAsync(
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
                                content.Content = XamlReader.Load(fileReload.Content);
                                break;
                        }
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
#if __IOS__ || __ANDROID__
						case ScrollContentPresenter scrollPresenter:
							yield return EnumerateInstances(scrollPresenter.Content, baseUri);
							break;
#endif
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
