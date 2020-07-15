using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Uno.UI.Extensions
{
	internal static class ThicknessExtensions
	{
		/// <summary>
		/// Is this <see cref="Thickness"/> uniform (all sides are equal)?
		/// </summary>
		public static bool IsUniform(this Thickness thickness) => thickness.Left == thickness.Top && thickness.Left == thickness.Right && thickness.Left == thickness.Bottom;
	}
}
