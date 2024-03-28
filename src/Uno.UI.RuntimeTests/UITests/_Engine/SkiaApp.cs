#nullable enable

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
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests;
using Uno.UITest.Helpers.Queries;

namespace Uno.UITest;

public partial class SkiaApp : IApp
{
	private static SkiaApp? _current; // Make sure to not create multiple instances of the app (with a single InputInjector instance)!
	internal static SkiaApp Current => _current ??= new();

	private readonly InputInjector _input;
	private MouseHelper Mouse { get; }

	private SkiaApp()
	{
		_input = InputInjector.TryCreate() ?? throw new InvalidOperationException("Cannot create input injector");
		Mouse = new MouseHelper(_input);

		CurrentPointerType = DefaultPointerType;

		// Does not supports parallel testing ... but we are running on the UI thread anyway, we cannot use concurrent testing!
		Uno.UITest.Helpers.Queries.Helpers.App = this;
	}

	/// <summary>
	/// Gets the default pointer type for the current platform
	/// </summary>
	public PointerDeviceType DefaultPointerType => PointerDeviceType.Mouse;

	public PointerDeviceType CurrentPointerType { get; private set; }

	public async Task RunAsync(string metadataName)
	{
		var assemblyName = "SamplesApp.Skia";
		if (TestServices.WindowHelper.IsXamlIsland)
		{
			assemblyName = "UnoIslands" + assemblyName;
		}
		if (Type.GetType($"{metadataName}, {assemblyName}") is { } sampleType
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

			// Give ability to visually render the content ...
			// TODO: Make it reliable instead of that random value!
			await Task.Delay(33);
		}
		else
		{
			throw new InvalidOperationException($"Failed to run sample '{metadataName}'");
		}
	}

	QueryResult[] IApp.Query(string marked) => Query(marked);
	internal QueryResult[] Query(string marked)
		=> Query(QueryEx.Any.Marked(marked));

	QueryResult[] IApp.Query(IAppQuery query) => Query(query);
	internal QueryResult[] Query(IAppQuery query)
	{
		var all = TestServices.WindowHelper.WindowContent
			.GetAllChildren(includeCurrent: true)
			.OfType<FrameworkElement>()
			.Select(elt => new QueryResult(elt));

		return query.Execute(all).ToArray();
	}

	QueryResult[] IApp.Query(Func<IAppQuery, IAppQuery> query) => Query(query);
	internal QueryResult[] Query(Func<IAppQuery, IAppQuery> query)
	{
		var all = TestServices.WindowHelper.WindowContent
			.GetAllChildren(includeCurrent: true)
			.OfType<FrameworkElement>()
			.Select(elt => new QueryResult(elt));

		return query(QueryEx.Any).Execute(all).ToArray();
	}

	public void CleanupPointers()
	{
		InjectMouseInput(Mouse.ReleaseAny());
		InjectMouseInput(Mouse.MoveTo(0, 0));
	}

	public IDisposable SetPointer(PointerDeviceType type)
	{
		var previous = CurrentPointerType;
		CurrentPointerType = type;

		return new DisposableAction(() => CurrentPointerType = previous);
	}

	public void TapCoordinates(double x, double y)
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
								PositionY = (int)y
							},
							PointerOptions = InjectedInputPointerOptions.FirstButton
								| InjectedInputPointerOptions.PointerUp
						}
					}
				});
				_input.UninitializeTouchInjection();
				break;

			case PointerDeviceType.Mouse:
				InjectMouseInput(Mouse.ReleaseAny());
				InjectMouseInput(Mouse.MoveTo(x, y));
				InjectMouseInput(Mouse.Press());
				InjectMouseInput(Mouse.Release());
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
								| InjectedInputPointerOptions.InRange
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
									| InjectedInputPointerOptions.InRange
							}
						};
					}

					yield return new()
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

			case PointerDeviceType.Mouse:
				InjectMouseInput(Mouse.ReleaseAny());
				InjectMouseInput(Mouse.MoveTo(fromX, fromY));
				InjectMouseInput(Mouse.Press());
				InjectMouseInput(Mouse.MoveTo(toX, toY));
				InjectMouseInput(Mouse.Release());
				break;

			default:
				throw NotSupported();
		}
	}

	private void InjectMouseInput(IEnumerable<InjectedInputMouseInfo?> input)
		=> _input.InjectMouseInput(input.Where(i => i is not null).Cast<InjectedInputMouseInfo>());

	private void InjectMouseInput(params InjectedInputMouseInfo?[] input)
		=> _input.InjectMouseInput(input.Where(i => i is not null).Cast<InjectedInputMouseInfo>());

	private Exception NotSupported([CallerMemberName] string operation = "")
		=> new NotSupportedException($"'{operation}' with type '{CurrentPointerType}' is not supported yet on this platform. Feel free to contribute!");
}
