// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// DXamlCore.h, DXamlCore.cpp

#nullable enable

using System;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;

namespace Uno.UI.Xaml.Core
{
	internal class DXamlCore
	{
		private static readonly Lazy<DXamlCore> _current = new Lazy<DXamlCore>();

		public static DXamlCore Current => _current.Value;

		public CoreServices GetHandle() => CoreServices.Instance;

		public Rect DipsToPhysicalPixels(float scale, Rect dipRect)
		{
			var physicalRect = dipRect;
			physicalRect.X = dipRect.X * scale;
			physicalRect.Y = dipRect.Y * scale;
			physicalRect.Width = dipRect.Width * scale;
			physicalRect.Height = dipRect.Height * scale;
			return physicalRect;
		}

		// TODO Uno: Application-wide bar is not supported yet.
		public ApplicationBarService? TryGetApplicationBarService() => null;
	}
}
