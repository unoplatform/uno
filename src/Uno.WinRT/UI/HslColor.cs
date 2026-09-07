using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.UI;

internal struct HslColor(double h, double s, double l)
{
	public double H { get; set; } = h;
	public double S { get; set; } = s;
	public double L { get; set; } = l;
}
