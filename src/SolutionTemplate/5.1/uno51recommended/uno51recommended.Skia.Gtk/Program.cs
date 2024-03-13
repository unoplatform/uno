using System;
using GLib;
using Uno.UI.Runtime.Skia.Gtk;

namespace uno51recommended.Skia.Gtk;

public class Program
{
    public static void Main(string[] args)
    {
        ExceptionManager.UnhandledException += delegate (UnhandledExceptionArgs expArgs)
        {
            Console.WriteLine("GLIB UNHANDLED EXCEPTION" + expArgs.ExceptionObject.ToString());
            expArgs.ExitApplication = true;
        };

        var host = new GtkHost(() => new AppHead());

        host.Run();
    }
}
