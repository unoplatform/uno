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
		private string? _lastUpdatedFilePath;
		private bool _supportsXamlReader;

#if __IOS__ || __CATALYST__ || __ANDROID__
		private bool? _isIssue93860Fixed;
#endif

		private void InitializeXamlReader()
		{
			var targetFramework = GetMSBuildProperty("TargetFramework");
			var buildingInsideVisualStudio = GetMSBuildProperty("BuildingInsideVisualStudio").Equals("true", StringComparison.OrdinalIgnoreCase);

			// As of VS 17.8, the only target which supports 
			//
			// Disabled until https://github.com/dotnet/runtime/issues/93860 is fixed
			//
			_supportsXamlReader =
				!IsIssue93860Fixed()
				&& (targetFramework.Contains("-android", StringComparison.OrdinalIgnoreCase)
					|| targetFramework.Contains("-ios", StringComparison.OrdinalIgnoreCase)
					|| targetFramework.Contains("-maccatalyst", StringComparison.OrdinalIgnoreCase));

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"XamlReader Hot Reload Enabled:{_supportsXamlReader} " +
					$"targetFramework:{targetFramework}");
			}
		}

		private bool IsIssue93860Fixed()
		{
#if __IOS__ || __CATALYST__ || __ANDROID__

#if __IOS__ || __CATALYST__
			var assembly = typeof(global::Foundation.NSObject).GetTypeInfo().Assembly;
#elif __ANDROID__
			var assembly = typeof(global::Android.Views.ViewGroup).GetTypeInfo().Assembly;
#endif
			if (_isIssue93860Fixed is null)
			{
				// If we can't find or parse the version attribute, we're assuming #93860 is fixed.
				_isIssue93860Fixed = true;

				if (assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>() is AssemblyInformationalVersionAttribute aiva)
				{
#if __IOS__ || __CATALYST__
					if (this.Log().IsEnabled(LogLevel.Trace))
					{
						this.Log().Trace($"iOS/Catalyst do not support C# based XAML hot reload: https://github.com/unoplatform/uno/issues/15918");
					}

					return false;
#else
					if (this.Log().IsEnabled(LogLevel.Trace))
					{
						this.Log().Trace($".NET Platform Bindings Version: {aiva.InformationalVersion}");
					}

					// From 8.0.100 assemblies:
					// 34.0.1.42; git-rev-head:f1b7113; git-branch:release/8.0.1xx

					var parts = aiva.InformationalVersion.Split(';');

					if (parts.Length > 0 && Version.TryParse(parts[0], out var version))
					{
						_isIssue93860Fixed = version >=
#if __ANDROID__
							new Version(34, 0, 1, 52); // 8.0.102
#endif

						if (!_isIssue93860Fixed.Value && this.Log().IsEnabled(LogLevel.Warning))
						{
							this.Log().Warn(
								$"The .NET Platform Bindings version {version} is too old " +
								$"and contains this issue: https://github.com/dotnet/runtime/issues/93860. " +
								$"Make sure to upgrade to .NET 8.0.102 or later");
						}
					}
#endif
				}
			}

			return _isIssue93860Fixed.Value;
#else
			// XAML Reader should not be used for non-mobile targets.
			return false;
#endif
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
