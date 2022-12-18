using System;
using SkiaSharp.Views.Tizen.NUI;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;
using Uno.UI.Runtime.Skia;

namespace TizenSamplesApp.Tizen
{
	internal class Program
	{
		//public void OnKeyEvent(object sender, Window.KeyEventArgs e)
		//{
		//	if (e.Key.State == Key.StateType.Down && (e.Key.KeyPressedName == "XF86Back" || e.Key.KeyPressedName == "Escape"))
		//	{
		//		Exit();
		//	}
		//}

		static void Main(string[] args)
		{
			var host = new TizenHost(() => new SamplesApp.App());
			host.Run();
		}
	}
}
