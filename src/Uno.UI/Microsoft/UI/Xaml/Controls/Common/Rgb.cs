namespace Microsoft.UI.Xaml.Controls
{
	// Uno Doc: Originally defined in ColorConversion.h in WinUI but moved here to a separate file.
	internal struct Rgb
	{
		public double R { get; set; }
		public double G { get; set; }
		public double B { get; set; }

		public Rgb(double r, double g, double b)
		{
			this.R = r;
			this.G = g;
			this.B = b;
		}
	}
}
