using System;
using Uno.UI.Runtime.Skia.Linux.FrameBuffer;

namespace MyAppXamlTrim
{
	public sealed class Program
	{
		static void Main(string[] args)
		{
			try
			{
				Console.CursorVisible = false;

				var host = new FrameBufferHost(() => new App());
				host.Run();
			}
			finally
			{
				Console.CursorVisible = true;
			}
		}
	}
}
