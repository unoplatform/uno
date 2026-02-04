// This file is included in both Uno.UWP (internal as Windows.whatever) and Uno.UI (public as Microsoft.whatever)
using System;
using System.Collections.Generic;
using System.Text;

#if IS_UNO_UI_PROJECT
using Windows.UI.Text;
namespace Microsoft.UI.Text
#else
namespace Windows.UI.Text
#endif
{
#if !IS_UNO_UI_PROJECT
	internal
#else
	public
#endif
	partial class FontWeights
	{
		private static FontWeight? _thin;
		private static FontWeight? _extraLight;
		private static FontWeight? _light;
		private static FontWeight? _semiLight;
		private static FontWeight? _normal;
		private static FontWeight? _medium;
		private static FontWeight? _semiBold;
		private static FontWeight? _bold;
		private static FontWeight? _extraBold;
		private static FontWeight? _black;
		private static FontWeight? _extraBlack;

		public static FontWeight Thin => _thin ??= new FontWeight(100);
		public static FontWeight ExtraLight => _extraLight ??= new FontWeight(200);
		public static FontWeight UltraLight => _extraLight ??= new FontWeight(200);
		public static FontWeight Light => _light ??= new FontWeight(300);
		public static FontWeight SemiLight => _semiLight ??= new FontWeight(350);
		public static FontWeight Normal => _normal ??= new FontWeight(400);
		public static FontWeight Regular => _normal ??= new FontWeight(400);
		public static FontWeight Medium => _medium ??= new FontWeight(500);
		public static FontWeight SemiBold => _semiBold ??= new FontWeight(600);
		public static FontWeight DemiBold => _semiBold ??= new FontWeight(600);
		public static FontWeight Bold => _bold ??= new FontWeight(700);
		public static FontWeight ExtraBold => _extraBold ??= new FontWeight(800);
		public static FontWeight UltraBold => _extraBold ??= new FontWeight(800);
		public static FontWeight Black => _black ??= new FontWeight(900);
		public static FontWeight Heavy => _black ??= new FontWeight(900);
		public static FontWeight ExtraBlack => _extraBlack ??= new FontWeight(950);
		public static FontWeight UltraBlack => _extraBlack ??= new FontWeight(950);
	}
}
