using System;
using GLib;
using Uno.UI.Runtime.Skia;

namespace UnoQuickStart.Skia.Gtk
{
	class Program
	{
		static void Main(string[] args)
		{
			ExceptionManager.UnhandledException += delegate (UnhandledExceptionArgs expArgs)
			{
				Console.WriteLine("GLIB UNHANDLED EXCEPTION" + expArgs.ExceptionObject.ToString());
				expArgs.ExitApplication = true;
			};

			Windows.ApplicationModel.Resources.ResourceLoader.GetStringInternal = s => null;

			var host = new GtkHost(() => new App(), args);

			host.Run();
		}
	}
}
