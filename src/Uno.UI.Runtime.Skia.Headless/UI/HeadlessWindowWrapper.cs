#nullable enable

using System;
using Windows.Foundation;
using Windows.Graphics;
using Uno.UI.Dispatching;
using Uno.UI.Xaml.Controls;

namespace Uno.WinUI.Runtime.Skia.Headless.UI;

internal class HeadlessWindowWrapper : NativeWindowWrapperBase
{
	private static HeadlessWindowWrapper? _instance;

	internal static HeadlessWindowWrapper Instance => _instance!;

	public static void Init(int width, int height, float scale) => _instance = new(width, height, scale);

	private HeadlessWindowWrapper(int width, int height, float scale)
	{
		if (_instance != null)
		{
			throw new InvalidOperationException($"{nameof(HeadlessWindowWrapper)} should be created once.");
		}
		_instance = this;

		RawWidth = width;
		RawHeight = height;
		Scale = scale;
	}

	public override object? NativeWindow => null;

	/// <summary>Buffer/screen width in raw pixels.</summary>
	internal int RawWidth { get; }

	/// <summary>Buffer/screen height in raw pixels.</summary>
	internal int RawHeight { get; }

	/// <summary>Rasterization scale applied to the logical bounds.</summary>
	internal float Scale { get; }

	internal void ApplySize()
	{
		if (XamlRoot is { })
		{
			RasterizationScale = Scale;
			var bounds = new Rect(0, 0, RawWidth / Scale, RawHeight / Scale);
			SetBoundsAndVisibleBounds(bounds, bounds);
			var rawSize = new SizeInt32(RawWidth, RawHeight);
			SetSizes(rawSize, rawSize);
		}
		else
		{
			NativeDispatcher.Main.Enqueue(ApplySize);
		}
	}
}
