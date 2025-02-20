using System;
using System.Linq;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Input.Keyboard
{
	[Sample("Keyboard", IsManualTest = true)]
	public sealed partial class Keyboard_Modifiers : Page
	{
#if HAS_UNO
		private DispatcherTimer _timer;
#endif

		public Keyboard_Modifiers()
		{
			this.InitializeComponent();
#if HAS_UNO
			_timer = new DispatcherTimer();
			_timer.Interval = TimeSpan.FromMilliseconds(100);
			_timer.Tick += (_, _) =>
			{
				var mods = PlatformHelpers.GetKeyboardModifiers();
				var modString = "";
				if (mods.HasFlag(VirtualKeyModifiers.Shift))
				{
					modString += " Shift";
				}
				if (mods.HasFlag(VirtualKeyModifiers.Control))
				{
					modString += " Ctrl";
				}
				if (mods.HasFlag(VirtualKeyModifiers.Windows))
				{
					modString += " Win";
				}
				if (mods.HasFlag(VirtualKeyModifiers.Menu))
				{
					modString += " Menu";
				}

				if (string.IsNullOrEmpty(modString))
				{
					modString = "None";
				}
				statusTb.Text = $"Modifiers pressed: {modString}";
			};

			Loaded += (_, _) => _timer.Start();
			Unloaded += (_, _) => _timer.Stop();
#endif
		}
	}
}
