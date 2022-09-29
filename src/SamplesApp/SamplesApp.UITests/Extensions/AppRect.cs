#nullable disable

using Uno.UITest;

#if IS_RUNTIME_UI_TESTS
using Windows.Foundation;
#endif

namespace SamplesApp.UITests;

#if IS_RUNTIME_UI_TESTS
internal struct AppRect : IAppRect
{
	private readonly Rect _value;

	public AppRect(Rect rect)
		=> _value = rect;

	public AppRect(Point point, Size size)
		=> _value = new Rect(point, size);

	public AppRect(double x, double y, double width, double height)
		=> _value = new Rect(x, y, width, height);

	public static implicit operator AppRect(Rect rect)
		=> new(rect);

	public static implicit operator Rect(AppRect rect)
		=> rect._value;

	public double X => _value.X;
	public double Y => _value.Y;
	public double Left => _value.Left;
	public double Right => _value.Right;
	public double Top => _value.Top;
	public double Bottom => _value.Bottom;
	public double Width => _value.Width;
	public double Height => _value.Height;

	public double CenterX => _value.X + _value.Width / 2;
	public double CenterY => _value.Y + _value.Height / 2;

	float IAppRect.Width => (float)Width;
	float IAppRect.Height => (float)Height;
	float IAppRect.X => (float)X;
	float IAppRect.Y => (float)Y;
	float IAppRect.CenterX => (float)CenterX;
	float IAppRect.CenterY => (float)CenterY;
	float IAppRect.Right => (float)Right;
	float IAppRect.Bottom => (float)Bottom;
}
#else
public partial class AppRect : IAppRect
{
	public AppRect(float x, float y, float width, float height)
	{
		X = x;
		Y = y;
		Width = width;
		Height = height;
	}

	public float Width { get; }
	public float Height { get; }
	public float X { get; }
	public float Y { get; }
	public float CenterX => Width / 2f + X;
	public float CenterY => Height / 2f + Y;
	public float Right => X + Width;
	public float Bottom => Y + Height;
}
#endif
