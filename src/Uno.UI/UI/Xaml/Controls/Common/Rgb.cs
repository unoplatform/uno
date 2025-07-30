namespace Microsoft.UI.Xaml.Controls
{
	// Uno Doc: Originally defined in ColorConversion.h in WinUI but moved here to a separate file.
	internal struct Rgb
	{
		// Uno Doc: Intentionally use fields to match C++ and allow access to the variable by ref.
		public double R;
		public double G;
		public double B;

		public Rgb(double r, double g, double b)
		{
			this.R = r;
			this.G = g;
			this.B = b;
		}
	}
}
