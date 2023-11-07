using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Uno.UI.Xaml;
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
		private string? _lastUpdatedFilePath;
		private bool _supportsXamlReader;

		private void InitializeXamlReader()
		{
			var targetFramework = GetMSBuildProperty("TargetFramework");
			var buildingInsideVisualStudio = GetMSBuildProperty("BuildingInsideVisualStudio").Equals("true", StringComparison.OrdinalIgnoreCase);

			// As of VS 17.8, the only target which supports 
			//
			// Disabled until https://github.com/dotnet/runtime/issues/93860 is fixed
			//
			_supportsXamlReader = (targetFramework.Contains("-android", StringComparison.OrdinalIgnoreCase)
				|| targetFramework.Contains("-ios", StringComparison.OrdinalIgnoreCase)
				|| targetFramework.Contains("-maccatalyst", StringComparison.OrdinalIgnoreCase));

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"XamlReader Hot Reload Enabled:{_supportsXamlReader} " +
					$"targetFramework:{targetFramework}");
			}
		}

		private void ReloadFileWithXamlReader(FileReload fileReload)
		{
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

					RemoteControlClient.Instance?.NotifyOfEvent(nameof(FileReload), fileReload.FilePath);
				});
		}

		private async Task ReloadWithFileAndContent(string filePath, string fileContent)
		{
			try
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().LogDebug($"XamlReader reloading changed file [{filePath}]");
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

				foreach (var window in ApplicationHelper.Windows)
				{
					if (window.Content is null)
					{
						return;
					}

					foreach (var instance in EnumerateInstances(window.Content, IsSameBaseUri).OfType<FrameworkElement>())
					{
						if (XamlReader.LoadUsingXClass(fileContent, uri.ToString()) is FrameworkElement newContent)
						{
							SwapViews(instance, newContent);
						}
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
	}
}
