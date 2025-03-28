// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.Foundation;
using Windows.UI.Xaml;
using Uno.UI;

namespace DirectUI
{
	internal class CSizeUtil
	{
		public static void Deflate(
			ref Size pSize,
			Thickness thickness)
			=> pSize = pSize.Subtract(thickness);

		public static void Inflate(
			ref Size pSize,
			Thickness thickness)
			=> pSize = pSize.Add(thickness);

		public static void Deflate(
			ref Size pSize,
			Size size)
			=> pSize = pSize.Subtract(size);

		public static void Inflate(
			ref Size pSize,
			Size size)
			=> pSize = pSize.Add(size);

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
