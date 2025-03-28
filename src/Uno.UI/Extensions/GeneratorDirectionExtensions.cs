using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Controls.Primitives;

namespace Uno.UI.Extensions
{
	internal static class GeneratorDirectionExtensions
	{
		public static GeneratorDirection Inverse(this GeneratorDirection generatorDirection) => generatorDirection switch
		{
			GeneratorDirection.Forward => GeneratorDirection.Backward,
			GeneratorDirection.Backward => GeneratorDirection.Forward,
			_ => throw new ArgumentOutOfRangeException()
		};
	}
}
