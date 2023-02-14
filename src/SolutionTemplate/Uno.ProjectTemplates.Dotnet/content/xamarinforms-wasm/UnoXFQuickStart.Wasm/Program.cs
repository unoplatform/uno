using System;
using Microsoft.UI.Xaml;
using Microsoft.Extensions.Logging;

namespace UnoXFQuickStart.Wasm
{
	public sealed class Program
	{
		static int Main(string[] args)
		{
			ConfigureFilters(Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory);

			Microsoft.UI.Xaml.Application.Start(_ => new UnoXFQuickStart.UWP.App());

			return 0;
		}

		static void ConfigureFilters(ILoggerFactory factory)
		{
			//-:cnd:noEmit
#if DEBUG
			factory
				.WithFilter(new FilterLoggerSettings
					{
						{ "Uno", LogLevel.Warning },
						{ "Windows", LogLevel.Warning },

						// Generic Xaml events
						// { "Microsoft.UI.Xaml", LogLevel.Debug },
						// { "Microsoft.UI.Xaml.Shapes", LogLevel.Debug },
						// { "Microsoft.UI.Xaml.VisualStateGroup", LogLevel.Debug },
						// { "Microsoft.UI.Xaml.StateTriggerBase", LogLevel.Debug },
						// { "Microsoft.UI.Xaml.UIElement", LogLevel.Debug },
						// { "Microsoft.UI.Xaml.Setter", LogLevel.Debug },

						// Layouter specific messages
						// { "Microsoft.UI.Xaml.Controls", LogLevel.Debug },
						// { "Microsoft.UI.Xaml.Controls.Layouter", LogLevel.Debug },
						// { "Microsoft.UI.Xaml.Controls.Panel", LogLevel.Debug },

						// Binding related messages
						// { "Microsoft.UI.Xaml.Data", LogLevel.Debug },

						//  Binder memory references tracking
						// { "ReferenceHolder", LogLevel.Debug },
					}
				)
				.AddConsole(LogLevel.Trace);
#else
			factory
				.AddConsole(LogLevel.Error);
#endif

#if HAS_UNO
			global::Uno.UI.Adapter.Microsoft.Extensions.Logging.LoggingAdapter.Initialize();
#endif
			//-:cnd:noEmit
		}
	}
}
