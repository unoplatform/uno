using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Wasm;

namespace Windows.UI.Xaml.Wasm
{
	/// <summary>
	/// Defines 'defs' element that hosts non-visual elements inside an SVG.
	/// </summary>
	internal partial class DefsSvgElement : SvgElement
	{
		public UIElementCollection Defs { get; }

		public DefsSvgElement() : base("defs")
		{
			Defs = new UIElementCollection(this);
		}
	}
}
