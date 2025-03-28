using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;

namespace Uno.UI.Extensions
{
	internal static class ThicknessExtensions
	{
		/// <summary>
		/// Is this <see cref="Thickness"/> uniform (all sides are equal)?
		/// </summary>
		public static bool IsUniform(this Thickness thickness)
			=> thickness.Left == thickness.Top && thickness.Left == thickness.Right && thickness.Left == thickness.Bottom;

		public static Thickness Minus(this Thickness x, Thickness y) // Minus ==> There is a (not implemented) Substract on struct in mono!
			=> new Thickness(x.Left - y.Left, x.Top - y.Top, x.Right - y.Right, x.Bottom - y.Bottom);

		public static double Horizontal(this Thickness thickness)
			=> thickness.Left + thickness.Right;

		public static double Vertical(this Thickness thickness)
			=> thickness.Top + thickness.Bottom;
	}
}
