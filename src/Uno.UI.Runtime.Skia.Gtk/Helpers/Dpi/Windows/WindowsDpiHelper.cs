using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.Runtime.Skia.Helpers
{
	// Borrowed from https://stackoverflow.com/questions/43537990/wpf-clickonce-dpi-awareness-per-monitor-v2
	public class WindowsDpiHelper
	{
		private WindowsDpiHelper()
		{ }

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool SetProcessDpiAwarenessContext(int dpiFlag);

		[DllImport("SHCore.dll", SetLastError = true)]
		private static extern bool SetProcessDpiAwareness(PROCESS_DPI_AWARENESS awareness);

		[DllImport("user32.dll")]
		private static extern bool SetProcessDPIAware();

		private enum PROCESS_DPI_AWARENESS
		{
			Process_DPI_Unaware = 0,
			Process_System_DPI_Aware = 1,
			Process_Per_Monitor_DPI_Aware = 2
		}

		private enum DPI_AWARENESS_CONTEXT
		{
			DPI_AWARENESS_CONTEXT_UNAWARE = 16,
			DPI_AWARENESS_CONTEXT_SYSTEM_AWARE = 17,
			DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE = 18,
			DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = 34
		}

		public static void SetupDpiAwareness()
		{
			if (Environment.OSVersion.Version >= new Version(6, 3, 0)) // Windows 8.1
			{
				if (Environment.OSVersion.Version >= new Version(10, 0, 15063)) // Windows 10 1703
				{
					SetProcessDpiAwarenessContext((int)DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
				}
				else
				{
					SetProcessDpiAwareness(PROCESS_DPI_AWARENESS.Process_Per_Monitor_DPI_Aware);
				}
			}
			else
			{
				SetProcessDPIAware();
			}
		}
	}
}
