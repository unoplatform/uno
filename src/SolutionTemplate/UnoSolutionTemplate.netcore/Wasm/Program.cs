using System;
using Microsoft.UI.Xaml;

namespace $ext_safeprojectname$.Wasm
{
	public sealed class Program
	{
		private static App _app;

		static int Main(string[] args)
		{
			Microsoft.UI.Xaml.Application.Start(_ => _app = new App());

			return 0;
		}
	}
}
