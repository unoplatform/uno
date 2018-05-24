using System;
using System.IO;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Shapes;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.Foundation;
using Uno.Logging;
using Windows.UI.Popups;
using System.Reflection;

namespace Uno.UI.Wasm.Shell
{
	class Program
	{
		private static App.App _app;

		static void Main(string[] args)
		{
			ConfigureFilters(LogExtensionPoint.AmbientLoggerFactory);

			new App.App();
		}

		static void ConfigureFilters(ILoggerFactory factory)
		{ 
			factory
				.WithFilter(new FilterLoggerSettings
					{
						{ "Uno", LogLevel.Warning },
						{ "Windows", LogLevel.Warning },
						{ "SampleControl.Presentation", LogLevel.Debug },

						// Generic Xaml events
						// { "Windows.UI.Xaml", LogLevel.Debug },

						// { "Uno.UI.Controls.AsyncValuePresenter", LogLevel.Debug },
						// { "Uno.UI.Controls.IfDataContext", LogLevel.Debug },
						   
						// Layouter specific messages
						// { "Windows.UI.Xaml.Controls", LogLevel.Debug },
						//{ "Windows.UI.Xaml.Controls.Layouter", LogLevel.Debug },
						//{ "Windows.UI.Xaml.Controls.Panel", LogLevel.Debug },
						   
						// Binding related messages
						// { "Windows.UI.Xaml.Data", LogLevel.Debug },
						// { "Windows.UI.Xaml.Data", LogLevel.Debug },
						   
						//  Binder memory references tracking
						// { "ReferenceHolder", LogLevel.Debug },
					}
				)
				.AddConsole(LogLevel.Debug);
		}
	}
}
