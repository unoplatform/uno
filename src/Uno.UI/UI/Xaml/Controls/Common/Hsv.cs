using System.Numerics;

namespace Microsoft.UI.Xaml.Controls
{
	// Uno Doc: Originally defined in ColorConversion.h in WinUI but moved here to a separate file.
	internal struct Hsv
	{
		// Uno Doc: Intentionally use fields to match C++ and allow access to the variable by ref.
		// Ref local variables are needed in ColorHelpers.cs to match C++ pointer usage.
		public double H;
		public double S;
		public double V;

		public Hsv(double h, double s, double v)
		{
			this.H = h;
			this.S = s;
			this.V = v;
		}

		// Uno Docs: The following methods were originally defined in ColorConversion.h in a separate "hsv" namespace.
		// They had to be moved into a class for C# and here in the Hsv class made the most sense.
		public static float GetHue(Vector4 hsva) { return hsva.X; }
		public static void SetHue(Vector4 hsva, float hue) { hsva.X = hue; }
		public static float GetSaturation(Vector4 hsva) { return hsva.Y; }
		public static void SetSaturation(Vector4 hsva, float saturation) { hsva.Y = saturation; }
		public static float GetValue(Vector4 hsva) { return hsva.Z; }
		public static void SetValue(Vector4 hsva, float value) { hsva.Z = value; }
		public static float GetAlpha(Vector4 hsva) { return hsva.W; }
		public static void SetAlpha(Vector4 hsva, float alpha) { hsva.W = alpha; }
	}
}
