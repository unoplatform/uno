using System;
using Windows.UI.Xaml;

namespace $ext_safeprojectname$.Wasm
{
	public class Program
	{
		private static App app;

		static int Main(string[] args)
		{
			Windows.UI.Xaml.Application.Start(_ => app = new App());

			return 0;
		}
	}
}
