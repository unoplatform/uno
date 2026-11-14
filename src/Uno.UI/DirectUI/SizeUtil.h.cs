// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference SizeUtil.cpp, tag winui3/release/2.5.1

using System;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Uno.UI;

namespace DirectUI
{
	internal class CSizeUtil
	{
		public static void Deflate(
			ref Size pSize,
			Thickness thickness)
			=> pSize = new Size(
				Math.Max(0.0, pSize.Width - (thickness.Left + thickness.Right)),
				Math.Max(0.0, pSize.Height - (thickness.Top + thickness.Bottom)));

		public static void Inflate(
			ref Size pSize,
			Thickness thickness)
			=> pSize = new Size(
				Math.Max(0.0, pSize.Width + (thickness.Left + thickness.Right)),
				Math.Max(0.0, pSize.Height + (thickness.Top + thickness.Bottom)));

		public static void Deflate(
			ref Size pSize,
			Size size)
			=> pSize = new Size(
				Math.Max(0.0, pSize.Width - size.Width),
				Math.Max(0.0, pSize.Height - size.Height));

		public static void Inflate(
			ref Size pSize,
			Size size)
			=> pSize = new Size(
				Math.Max(0.0, pSize.Width + size.Width),
				Math.Max(0.0, pSize.Height + size.Height));

		public static void Deflate(
			ref Rect pRect,
			Thickness thickness)
			=> pRect = pRect.DeflateBy(thickness);

		public static void Inflate(
			ref Rect pRect,
			Thickness thickness)
			=> pRect = pRect.InflateBy(thickness);

		//public static void CombineThicknesses(
		//	ref Thickness pThickness,
		//	Thickness thickness);
	}
}
