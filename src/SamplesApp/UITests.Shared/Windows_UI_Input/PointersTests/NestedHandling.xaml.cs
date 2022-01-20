using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Input.PointersTests
{
	[Sample(
		"Pointers",
		Description = "Press the pink square, move a bit and release while remaining on the pink square.",
		IgnoreInSnapshotTests = true)]
	public sealed partial class NestedHandling : Page
	{
		public NestedHandling()
		{
			this.InitializeComponent();

			int containerMoveCount = 0, nestedMoveCount = 0;
			var even = true;

			_nested.PointerPressed += (snd, e) =>
			{
				e.Handled = true;
				_result.Text = "";
				containerMoveCount = 0;
				nestedMoveCount = 0;
			};
			_nested.PointerMoved += (snd, e) =>
			{
				// We filter out half of the events to validate that handled events are not always invalidly bubbled.
				if (even) 
				{
					containerMoveCount++;
				}
				else
				{
					e.Handled = true;
				}

				even = !even;
			};

			_container.AddHandler(PointerPressedEvent, new PointerEventHandler((snd, e) => _result.Text += "Pressed SUCCESS"), handledEventsToo: true);
			_container.PointerPressed += (snd, e) => _result.Text = "Pressed FAIL";
			_container.PointerMoved += (snd, e) => nestedMoveCount++;
			_container.PointerReleased += (snd, e) => _result.Text += $" | Released {(nestedMoveCount == containerMoveCount ? "SUCCESS" : "FAIL")}";


			_container.AddHandler(
				PointerEnteredEvent,
				new PointerEventHandler((snd, e) =>
				{
					if (e.OriginalSource != _container)
					{
						_enterResult.Text = "FAILED";
					}
				}),
				handledEventsToo: true);
			_nested.PointerEntered += (snd, e) => _enterResult.Text = "SUCCESS";
			_container.AddHandler(
				PointerExitedEvent,
				new PointerEventHandler((snd, e) =>
				{
					if (e.OriginalSource != _container)
					{
						_exitResult.Text = "FAILED";
					}
				}),
				handledEventsToo: true);
			_nested.PointerExited += (snd, e) => _exitResult.Text = "SUCCESS";
		}
	}
}
