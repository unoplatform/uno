// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference MicaController.cpp, commit b2aab7e

#nullable enable

using System;
using System.IO;
using System.Threading.Tasks;
using Android.App;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Renderscripts;
using Windows.Storage;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Win = Windows.UI;

namespace Microsoft.UI.Xaml.Controls;

public partial class MicaController
{
	internal static readonly Win.Color DarkThemeColor = Win.Color.FromArgb(255, 32, 32, 32);
	internal const float DarkThemeTintOpacity = 0.8f;

	internal static readonly Win.Color LightThemeColor = Win.Color.FromArgb(255, 243, 243, 243);
	internal const float LightThemeTintOpacity = 0.5f;

	private RenderScript? _rs;

	internal bool SetTarget(Windows.UI.Xaml.Window xamlWindow)
	{
		_rs = RenderScript.Create(Application.Context);

		var context = Application.Context;
		var value = WallpaperManager.GetInstance(context)?.GetBuiltInDrawable(WallpaperManagerFlags.System);

		if (value is BitmapDrawable bitmapDrawable)
		{
			bitmapDrawable = ApplyMicaOnDrawable(bitmapDrawable, DarkThemeTintOpacity);
			_ = SetBackground(xamlWindow, bitmapDrawable);

			return true;
		}

		return false;
	}

	private BitmapDrawable ApplyMicaOnDrawable(BitmapDrawable bitmapDrawable, float darkenFactor)
	{
		var inputBitmap = bitmapDrawable.Bitmap!;
		var outputBitmap = inputBitmap.Copy(inputBitmap.GetConfig(), true)!;

		Android.Graphics.Canvas canvas = new Android.Graphics.Canvas(outputBitmap);

		var paint = new Paint();
		paint.SetARGB((int)(darkenFactor * 255), 32, 32, 32);

		canvas.DrawRect(0, 0, inputBitmap.Width, inputBitmap.Height, paint);

		var input = Allocation.CreateFromBitmap(_rs, outputBitmap);
		var output = Allocation.CreateFromBitmap(_rs, outputBitmap)!;

		//TODO ScriptIntrinsicBlur seems to be deprecated, need to find a replacement / custom implementation
		var script = ScriptIntrinsicBlur.Create(_rs, Element.U8_4(_rs))!;

		for (var i = 0; i < 120; i++)
		{
			script.SetRadius(25);
			script.SetInput(input);
			script.ForEach(output);
		}

		output.CopyTo(outputBitmap);

		var resources = Application.Context.Resources;

		return new BitmapDrawable(resources, outputBitmap);
	}

	private async Task<bool> SetBackground(Windows.UI.Xaml.Window xamlWindow, BitmapDrawable bitmapDrawable)
	{
		using var bitmap = bitmapDrawable.Bitmap!;
		using var stream = new MemoryStream();

		bitmap.Compress(Bitmap.CompressFormat.Png, 100, stream);
		byte[] byteArray = stream.ToArray();

		//TODO: Maybe there could be a better way to do this without needing to save the img to storage
		StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync("background.png", CreationCollisionOption.ReplaceExisting);
		using (Stream fileStream = await file.OpenStreamForWriteAsync())
		{
			await fileStream.WriteAsync(byteArray, 0, byteArray.Length);
		}

		ImageBrush backgroundImageBrush = new ImageBrush
		{
			ImageSource = new BitmapImage(new Uri(file.Path, UriKind.Absolute))
		};

		if (xamlWindow.RootElement is Panel panel)
		{
			panel.Background = backgroundImageBrush;

			return true;
		}

		return false;
	}
}
