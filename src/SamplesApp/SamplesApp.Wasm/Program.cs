using System;
using Windows.UI.Xaml;

namespace SamplesApp.Wasm
{
	public class Program
	{
		private static App _app;

		public static void Main(string[] args)
		{
			Windows.UI.Xaml.Application.Start(_ => _app = new App());
		}
	} 
}
