using System;
using System.Linq;
using Windows.Foundation;

namespace Uno.UITest.Helpers.Queries;

internal struct AppRect
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
}
