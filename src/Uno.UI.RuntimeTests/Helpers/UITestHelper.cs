#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Input.Preview.Injection;
using Windows.UI.Popups;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Markup;
using Private.Infrastructure;
using SamplesApp.UITests;

#if !HAS_UNO
using System.Runtime.InteropServices;
#endif
namespace Uno.UI.RuntimeTests.Helpers;

// Note: This file contains a bunch of helpers that are expected to be moved to the test engine among the pointer injection work

public static class UITestHelper
{
	public static async Task<Windows.Foundation.Rect> Load(FrameworkElement element)
	{
		TestServices.WindowHelper.WindowContent = element;
		await TestServices.WindowHelper.WaitForLoaded(element);
		await TestServices.WindowHelper.WaitForIdle();

		return element.GetAbsoluteBounds();
	}

	/// <summary>
	/// Takes a screen-shot of the given element.
	/// </summary>
	/// <param name="element">The element to screen-shot.</param>
	/// <param name="opaque">Indicates if the resulting image should be make opaque (i.e. all pixels has an opacity of 0xFF) or not.</param>
	/// <param name="scaling">Indicates the scaling strategy to apply for the image (when screen is not using a 1.0 scale, usually 4K screens).</param>
	/// <returns></returns>
	public static async Task<RawBitmap> ScreenShot(FrameworkElement element, bool opaque = false, UIHelper.ScreenShotScalingMode scaling = UIHelper.ScreenShotScalingMode.UsePhysicalPixelsWithImplicitScaling)
		=> new(await UIHelper.ScreenShot(element, opaque, scaling));
}

public class DynamicDataTemplate : IDisposable
{
	private static readonly Dictionary<Guid, Func<FrameworkElement>> _templates = new();

	public static FrameworkElement GetElement(string id)
		=> Guid.TryParse(id, out var guid) && _templates.TryGetValue(guid, out var factory)
			? factory()
			: new TextBlock { Text = $"Template '{id}' not found" };

	public DynamicDataTemplate(Func<FrameworkElement> factory)
	{
		_templates.Add(Id, factory);

#if HAS_UNO
		Value = new DataTemplate(factory);
#else
		Value = (DataTemplate)XamlReader.Load($@"
			<DataTemplate 
				xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
				xmlns:local=""using:{typeof(DynamicDataTemplatePresenter).Namespace}"">
				<local:DynamicDataTemplatePresenter TemplateId=""{Id}"" />
			</DataTemplate>");
#endif
	}

	public Guid Id { get; } = Guid.NewGuid();

	public DataTemplate Value { get; }

	public void Dispose()
		=> _templates.Remove(Id);

	~DynamicDataTemplate()
		=> Dispose();
}

public partial class DynamicDataTemplatePresenter : ContentPresenter
{
	public DynamicDataTemplatePresenter()
	{
		HorizontalAlignment = HorizontalAlignment.Stretch;
		VerticalAlignment = VerticalAlignment.Stretch;
		HorizontalContentAlignment = HorizontalAlignment.Stretch;
		VerticalContentAlignment = VerticalAlignment.Stretch;
	}

	public static readonly DependencyProperty TemplateIdProperty = DependencyProperty.Register(
		nameof(TemplateId),
		typeof(string),
		typeof(DynamicDataTemplatePresenter),
		new PropertyMetadata(default(string), OnTemplateIdChanged));

	private static void OnTemplateIdChanged(DependencyObject snd, DependencyPropertyChangedEventArgs args)
	{
		if (snd is DynamicDataTemplatePresenter that)
		{
			that.Content = args.NewValue is string id
				? DynamicDataTemplate.GetElement(id)
				: null;
		}
	}

	public string TemplateId
	{
		get => (string)this.GetValue(TemplateIdProperty);
		set => this.SetValue(TemplateIdProperty, value);
	}
}


public static class InputInjectorExtensions
{
	public static Finger GetFinger(this InputInjector injector, uint id = 42)
		=> new(injector, id);

#if !WINAPPSDK
	public static Mouse GetMouse(this InputInjector injector)
		=> new(injector);
#endif
}

public interface IInjectedPointer
{
	void Press(Point position);

	void MoveTo(Point position);

	void MoveBy(double deltaX = 0, double deltaY = 0);

	void Release();
}

public static class FrameworkElementExtensions
{
	public static Rect GetAbsoluteBounds(this FrameworkElement element)
		=> element.TransformToVisual(null).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
}

public static class PointExtensions
{
	public static Point Offset(this Point point, double xAndY)
		=> new(point.X + xAndY, point.Y + xAndY);

	public static Point Offset(this Point point, double x, double y)
		=> new(point.X + x, point.Y + y);
}

public static class InjectedPointerExtensions
{
	public static void Press(this IInjectedPointer pointer, double x, double y)
		=> pointer.Press(new(x, y));

	public static void MoveTo(this IInjectedPointer pointer, double x, double y)
		=> pointer.MoveTo(new(x, y));

	public static void Drag(this IInjectedPointer pointer, Point from, Point to)
	{
		pointer.Press(from);
		pointer.MoveTo(to);
		pointer.Release();
	}
}

public partial class Finger : IInjectedPointer, IDisposable
{
	private const uint _defaultMoveSteps = 10;

	private readonly InputInjector _injector;
	private readonly uint _id;

	private Point? _currentPosition;

	public Finger(InputInjector injector, uint id)
	{
		_injector = injector;
		_id = id;

		_injector.InitializeTouchInjection(InjectedInputVisualizationMode.Default);
	}

	public void Press(Point position)
	{
		if (_currentPosition is null)
		{
			Inject(GetPress(_id, position));
			_currentPosition = position;
		}
	}

	void IInjectedPointer.MoveTo(Point position) => MoveTo(position);
	public void MoveTo(Point position, uint steps = _defaultMoveSteps)
	{
		if (_currentPosition is { } current)
		{
			Inject(GetMove(current, position, steps));
			_currentPosition = position;
		}
	}

	void IInjectedPointer.MoveBy(double deltaX, double deltaY) => MoveBy(deltaX, deltaY);
	public void MoveBy(double deltaX, double deltaY, uint steps = _defaultMoveSteps)
	{
		if (_currentPosition is { } current)
		{
			MoveTo(current.Offset(deltaX, deltaY), steps);
		}
	}

	public void Release()
	{
		if (_currentPosition is { } current)
		{
			Inject(GetRelease(current));
			_currentPosition = null;
		}
	}

	public void Dispose()
	{
		Release();
		_injector.UninitializeTouchInjection();
	}

	public static InjectedInputTouchInfo GetPress(uint id, Point position)
		=> new()
		{
			PointerInfo = new()
			{
				PointerId = id,
				PixelLocation = At(position),
				PointerOptions = InjectedInputPointerOptions.New
					| InjectedInputPointerOptions.FirstButton
					| InjectedInputPointerOptions.PointerDown
					| InjectedInputPointerOptions.InContact
			}
		};

	public static IEnumerable<InjectedInputTouchInfo> GetMove(Point fromPosition, Point toPosition, uint steps = _defaultMoveSteps)
	{
		steps += 1; // We need to send at least the final location, but steps refers to the number of intermediate points

		var stepX = (toPosition.X - fromPosition.X) / steps;
		var stepY = (toPosition.Y - fromPosition.Y) / steps;
		for (var step = 1; step <= steps; step++)
		{
			yield return new()
			{
				PointerInfo = new()
				{
					PixelLocation = At(fromPosition.X + step * stepX, fromPosition.Y + step * stepY),
					PointerOptions = InjectedInputPointerOptions.Update
						| InjectedInputPointerOptions.FirstButton
						| InjectedInputPointerOptions.InContact
						| InjectedInputPointerOptions.InRange
				}
			};
		}
	}

	public static InjectedInputTouchInfo GetRelease(Point position)
		=> new()
		{
			PointerInfo = new()
			{
				PixelLocation = At(position),
				PointerOptions = InjectedInputPointerOptions.FirstButton
					| InjectedInputPointerOptions.PointerUp
			}
		};

	private void Inject(IEnumerable<InjectedInputTouchInfo> infos)
		=> _injector.InjectTouchInput(infos.ToArray());

	private void Inject(params InjectedInputTouchInfo[] infos)
		=> _injector.InjectTouchInput(infos);

	// Note: This a patch until Uno's pointer injection is being relative to the screen
	private static InjectedInputPoint At(Point position)
		=> At(position.X, position.Y);

#if !HAS_UNO
	[LibraryImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);
	[StructLayout(LayoutKind.Sequential)]
	private struct RECT
	{
		public int Left;
		public int Top;
		public int Right;
		public int Bottom;
	}

#endif

	private static InjectedInputPoint At(double x, double y)
#if HAS_UNO
		=> new() { PositionX = (int)x, PositionY = (int)y };
#else
	{
		RECT rect = new();
		GetWindowRect(WinRT.Interop.WindowNative.GetWindowHandle(TestServices.WindowHelper.CurrentTestWindow), ref rect);
		var scale = TestServices.WindowHelper.CurrentTestWindow.Content.XamlRoot.RasterizationScale;

		return new()
		{
			PositionX = (int)((rect.Left + x) * scale),
			PositionY = (int)((rect.Top + y) * scale),
		};
	}
#endif
}

#if !WINAPPSDK
public class Mouse : IInjectedPointer, IDisposable
{
	private readonly InputInjector _input;

	public Mouse(InputInjector input)
	{
		_input = input;
	}

	private Point Current => _input.Mouse.Position;

	public void Press(Point position)
		=> Inject(GetMoveTo(position.X, position.Y, null).Concat(new[] { GetPress() }));

	public void PressRight(Point position)
		=> Inject(GetMoveTo(position.X, position.Y, null).Concat(new[] { GetRightPress() }));

	internal void Press(Point position, VirtualKeyModifiers modifiers)
	{
		var infos = GetMoveTo(position.X, position.Y, null).Concat(new[] { GetPress() });
		Inject(infos.Select(info => (info, modifiers)));
	}

	public void Press()
		=> Inject(GetPress());

	public void Press(VirtualKeyModifiers modifiers)
		=> Inject((GetPress(), modifiers));

	public void Release(VirtualKeyModifiers modifiers)
		=> Inject((GetRelease(), modifiers));

	public void Release()
		=> Inject(GetRelease());

	public void PressRight()
		=> Inject(GetRightPress());

	public void ReleaseRight()
		=> Inject(GetRightRelease());

	public void ReleaseAny()
	{
		var options = default(InjectedInputMouseOptions);

		var current = _input.Mouse;
		if (current.Properties.IsLeftButtonPressed)
		{
			options |= InjectedInputMouseOptions.LeftUp;
		}

		if (current.Properties.IsMiddleButtonPressed)
		{
			options |= InjectedInputMouseOptions.MiddleUp;
		}

		if (current.Properties.IsRightButtonPressed)
		{
			options |= InjectedInputMouseOptions.RightUp;
		}

		if (current.Properties.IsXButton1Pressed)
		{
			options |= InjectedInputMouseOptions.XUp;
		}

		if (options != default)
		{
			Inject(new InjectedInputMouseInfo
			{
				TimeOffsetInMilliseconds = 1,
				MouseOptions = options
			});
		}
	}

	public void MoveBy(double deltaX, double deltaY)
		=> Inject(GetMoveBy(deltaX, deltaY));

	void IInjectedPointer.MoveTo(Point position) => MoveTo(position);
	public void MoveTo(Point position, uint? steps = null)
		=> Inject(GetMoveTo(position.X, position.Y, steps));

	public void WheelUp() => Wheel(ScrollContentPresenter.ScrollViewerDefaultMouseWheelDelta);
	public void WheelDown() => Wheel(-ScrollContentPresenter.ScrollViewerDefaultMouseWheelDelta);
	public void WheelRight() => Wheel(ScrollContentPresenter.ScrollViewerDefaultMouseWheelDelta, isHorizontal: true);
	public void WheelLeft() => Wheel(-ScrollContentPresenter.ScrollViewerDefaultMouseWheelDelta, isHorizontal: true);

	public void Wheel(double delta, bool isHorizontal = false, uint steps = 1)
		=> Inject(GetWheel(delta, isHorizontal, steps));

	private IEnumerable<InjectedInputMouseInfo> GetMoveTo(double x, double y, uint? steps)
	{
		var x0 = Current.X;
		var y0 = Current.Y;
		var deltaX = x - x0;
		var deltaY = y - y0;

		steps ??= (uint)Math.Min(Math.Max(Math.Abs(deltaX), Math.Abs(deltaY)), 512);
		if (steps is 0)
		{
			yield break;
		}

		// Could probably use Bresenham's algorithm if performance issues appear
		var stepX = deltaX / steps.Value;
		var stepY = deltaY / steps.Value;

		var prevPositionX = (int)Math.Round(x0);
		var prevPositionY = (int)Math.Round(y0);

		for (var i = 1; i <= steps; i++)
		{
			var newPositionX = (int)Math.Round(x0 + i * stepX);
			var newPositionY = (int)Math.Round(y0 + i * stepY);

			yield return GetMoveBy(newPositionX - prevPositionX, newPositionY - prevPositionY);

			prevPositionX = newPositionX;
			prevPositionY = newPositionY;
		}
	}

	private void Inject(IEnumerable<InjectedInputMouseInfo> infos)
		=> _input.InjectMouseInput(infos);

	private void Inject(IEnumerable<(InjectedInputMouseInfo, VirtualKeyModifiers)> infos)
		=> _input.InjectMouseInput(infos);

	private void Inject(params InjectedInputMouseInfo[] infos)
		=> _input.InjectMouseInput(infos);

	private void Inject(params (InjectedInputMouseInfo, VirtualKeyModifiers)[] infos)
		=> _input.InjectMouseInput(infos);

	public void Dispose()
		=> ReleaseAny();

	private static InjectedInputMouseInfo GetPress()
		=> new()
		{
			TimeOffsetInMilliseconds = 1,
			MouseOptions = InjectedInputMouseOptions.LeftDown,
		};

	private static InjectedInputMouseInfo GetRightPress()
		=> new()
		{
			TimeOffsetInMilliseconds = 1,
			MouseOptions = InjectedInputMouseOptions.RightDown,
		};

	private static InjectedInputMouseInfo GetMoveBy(double deltaX, double deltaY)
		=> new()
		{
			DeltaX = (int)deltaX,
			DeltaY = (int)deltaY,
			TimeOffsetInMilliseconds = 1,
			MouseOptions = InjectedInputMouseOptions.MoveNoCoalesce,
		};

	private static InjectedInputMouseInfo GetRelease()
		=> new()
		{
			TimeOffsetInMilliseconds = 1,
			MouseOptions = InjectedInputMouseOptions.LeftUp,
		};

	private static InjectedInputMouseInfo GetRightRelease()
		=> new()
		{
			TimeOffsetInMilliseconds = 1,
			MouseOptions = InjectedInputMouseOptions.RightUp,
		};

	public static IEnumerable<InjectedInputMouseInfo> GetWheel(double delta, bool isHorizontal, uint steps = 1)
	{
		if (steps is 0)
		{
			yield break;
		}

		var stepSize = delta / steps;

		var prev = 0;

		for (var i = 1; i <= steps; i++)
		{
			var current = (int)Math.Round(i * stepSize);

			yield return isHorizontal
				? new() { TimeOffsetInMilliseconds = 1, DeltaX = current - prev, MouseOptions = InjectedInputMouseOptions.HWheel }
				: new() { TimeOffsetInMilliseconds = 1, DeltaY = current - prev, MouseOptions = InjectedInputMouseOptions.Wheel };

			prev = current;
		}
	}
}
#endif
