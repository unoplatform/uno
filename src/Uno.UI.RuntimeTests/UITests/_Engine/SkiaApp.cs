using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Devices.Input;
using Windows.UI.Input.Preview.Injection;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Web.Http;
using Private.Infrastructure;
using Uno.UI.RuntimeTests;

namespace Uno.UITest;

public class SkiaApp
{
	private readonly InputInjector _input;

	public SkiaApp()
	{
		_input = InputInjector.TryCreate() ?? throw new InvalidOperationException("Cannot create input injector");

		CurrentPointerType = DefaultPointerType;
	}

	/// <summary>
	/// Gets the default pointer type for the current platform
	/// </summary>
	public PointerDeviceType DefaultPointerType => PointerDeviceType.Mouse;

	public PointerDeviceType CurrentPointerType { get; private set; }

	public async Task RunAsync(string metadataName)
	{
		if (Type.GetType(metadataName + ", SamplesApp.Skia") is { } sampleType
			&& Activator.CreateInstance(sampleType) is FrameworkElement sample)
		{
			var root = new Grid
			{
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Stretch,
				Children = { sample }
			};
			TestServices.WindowHelper.WindowContent = root;
			await TestServices.WindowHelper.WaitForLoaded(root);
			sample.UpdateLayout();
			await TestServices.WindowHelper.WaitForIdle();

			await Task.Delay(10);
		}
		else
		{
			throw new InvalidOperationException($"Failed to run sample '{metadataName}'");
		}
	}

	public IDisposable SetPointer(PointerDeviceType type)
	{
		var previous = CurrentPointerType;
		CurrentPointerType = type;

		return new DisposableAction(() => CurrentPointerType = previous);
	}

	public void TapCoordinates(float x, float y)
	{
		switch (CurrentPointerType)
		{
			case PointerDeviceType.Touch:
				_input.InitializeTouchInjection(InjectedInputVisualizationMode.Default);
				_input.InjectTouchInput(new[]
				{
					new InjectedInputTouchInfo
					{
						PointerInfo = new()
						{
							PointerId = 42,
							PixelLocation = new ()
							{
								PositionX = (int)x,
								PositionY = (int)y
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
								PositionX = (int)x,
								PositionY = (int)x
							},
							PointerOptions = InjectedInputPointerOptions.FirstButton
								| InjectedInputPointerOptions.PointerUp
						}
					}
				});
				_input.UninitializeTouchInjection();
				break;

			default:
				throw NotSupported();
		}
	}

	public void DragCoordinates(double fromX, double fromY, double toX, double toY)
	{
		switch (CurrentPointerType)
		{
			case PointerDeviceType.Touch:
				_input.InitializeTouchInjection(InjectedInputVisualizationMode.Default);
				_input.InjectTouchInput(Inputs());
				_input.UninitializeTouchInjection();

				IEnumerable<InjectedInputTouchInfo> Inputs()
				{
					yield return new()
					{
						PointerInfo = new()
						{
							PointerId = 42,
							PixelLocation = new()
							{
								PositionX = (int)fromX,
								PositionY = (int)fromY
							},
							PointerOptions = InjectedInputPointerOptions.New
								| InjectedInputPointerOptions.FirstButton
								| InjectedInputPointerOptions.PointerDown
								| InjectedInputPointerOptions.InContact
						}
					};

					var steps = 10;
					var stepX = (toX - fromX) / steps;
					var stepY = (toY - fromY) / steps;
					for (var step = 0; step <= steps; step++)
					{
						yield return new()
						{
							PointerInfo = new()
							{
								PixelLocation = new()
								{
									PositionX = (int)(fromX + step * stepX),
									PositionY = (int)(fromY + step * stepY)
								},
								PointerOptions = InjectedInputPointerOptions.Update
									| InjectedInputPointerOptions.FirstButton
									| InjectedInputPointerOptions.InContact
							}
						};
					}

					yield return new ()
					{
						PointerInfo = new()
						{
							PixelLocation =
							{
								PositionX = (int)toX,
								PositionY = (int)toY
							},
							PointerOptions = InjectedInputPointerOptions.FirstButton
								| InjectedInputPointerOptions.PointerUp
						}
					};
				}
				break;

			default:
				throw NotSupported();
		}
	}

	private Exception NotSupported([CallerMemberName] string operation = null)
		=> new NotSupportedException($"'{operation}' with type '{CurrentPointerType}' is not supported yet on this platform. Feel free to contribute!");
}
