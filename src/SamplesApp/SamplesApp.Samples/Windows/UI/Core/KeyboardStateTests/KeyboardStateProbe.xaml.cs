using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using Windows.System;
using Windows.UI.Core;

namespace UITests.Windows_UI_Core.KeyboardStateTests;

[Sample(
	"Windows.UI.Core",
	Name = "KeyboardStateProbe",
	IsManualTest = true,
	Description =
		"Reports CoreWindow.GetKeyState for the modifier keys and for the last key pressed. Used to " +
		"verify that a modifier whose key up never reaches the app - a browser-level focus steal, or " +
		"a mobile on-screen keyboard raising Shift during auto-capitalization - stops being reported " +
		"as held, and that a key held down reports a stable state while the OS repeats it.")]
public sealed partial class KeyboardStateProbe : Page
{
	private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(50) };
	private string _last = "";
	private VirtualKey _lastKey = VirtualKey.None;

	public KeyboardStateProbe()
	{
		this.InitializeComponent();

		_timer.Tick += OnTick;
		KeyDown += (_, e) => _lastKey = e.OriginalKey;

		Loaded += (_, _) => _timer.Start();
		Unloaded += (_, _) => _timer.Stop();
	}

	private void OnTick(object sender, object e)
	{
		var state =
			$"ctrl={CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Control)} " +
			$"shift={CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Shift)} " +
			$"menu={CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Menu)} " +
			$"last[{_lastKey}]={CoreWindow.GetForCurrentThread().GetKeyState(_lastKey)}";

		StateText.Text = state;

		if (state != _last)
		{
			_last = state;

			// Also logged so the state can be followed from a browser console or device log,
			// where the transition matters more than the current value.
			Console.WriteLine($"KEYSTATE {state}");
		}
	}
}
