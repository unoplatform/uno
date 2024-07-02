#if __SKIA__

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Input.Preview.Injection;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Extensions;
using Uno.UITest;
using static Private.Infrastructure.TestServices.WindowHelper;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Input_Preview_Injection;

[TestClass]
public class Given_InputInjector
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_InjectTouch()
	{
		if (TestServices.WindowHelper.IsXamlIsland)
		{
			// Input injection is not supported in XamlIslands
			return;
		}

		var target = new Border
		{
			Background = new SolidColorBrush(Colors.DeepPink),
			Width = 200,
			Height = 200,
		};

		var actual = new List<PointerRoutedEventArgs>();
		target.PointerEntered += (snd, args) => actual.Add(args);
		target.PointerPressed += (snd, args) => actual.Add(args);
		target.PointerMoved += (snd, args) => actual.Add(args);
		target.PointerReleased += (snd, args) => actual.Add(args);
		target.PointerExited += (snd, args) => actual.Add(args);

		WindowContent = target;
		await WaitForLoaded(target);
		await WaitForIdle();

		var targetLocation = target.TransformToVisual(null).TransformPoint(default);
		var injector = InputInjector.TryCreate();

		injector.InitializeTouchInjection(InjectedInputVisualizationMode.Default);
		injector.InjectTouchInput(new[]
		{
			new InjectedInputTouchInfo
			{
				PointerInfo = new()
				{
					PointerId = 42,
					PixelLocation = new ()
					{
						PositionX = (int)targetLocation.X + 100,
						PositionY = (int)targetLocation.Y + 100
					},
					PointerOptions = InjectedInputPointerOptions.New
						| InjectedInputPointerOptions.FirstButton
						| InjectedInputPointerOptions.PointerDown
						| InjectedInputPointerOptions.InContact
				}
			},
			new InjectedInputTouchInfo
			{
				PointerInfo = new()
				{
					PixelLocation =
					{
						PositionX = (int)targetLocation.X + 100 + 2,
						PositionY = (int)targetLocation.Y + 100 + 2
					},
					PointerOptions = InjectedInputPointerOptions.FirstButton
						| InjectedInputPointerOptions.Update
						| InjectedInputPointerOptions.InContact
				}
			},
			new InjectedInputTouchInfo
			{
				PointerInfo = new()
				{
					PixelLocation =
					{
						PositionX = (int)targetLocation.X + 100 + 2,
						PositionY = (int)targetLocation.Y + 100 + 2
					},
					PointerOptions = InjectedInputPointerOptions.FirstButton
						| InjectedInputPointerOptions.PointerUp
				}
			}
		});
		injector.UninitializeTouchInjection();

		Assert.AreNotEqual(0, actual.Count);
	}
}
#endif
