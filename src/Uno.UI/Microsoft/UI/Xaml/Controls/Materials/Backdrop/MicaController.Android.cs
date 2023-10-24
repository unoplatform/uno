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
using Android.Util;
using Uno.Helpers.Theming;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using static System.Net.Mime.MediaTypeNames;
using Win = Windows.UI;

namespace Microsoft.UI.Xaml.Controls;

public partial class MicaController
{
	internal static readonly Win.Color DarkThemeColor = Win.Color.FromArgb(255, 32, 32, 32);
	internal const float DarkThemeTintOpacity = 0.8f;

	internal static readonly Win.Color LightThemeColor = Win.Color.FromArgb(255, 243, 243, 243);
	internal const float LightThemeTintOpacity = 0.5f;

	internal bool SetTarget(Windows.UI.Xaml.Window xamlWindow)
	{
		var context = Application.Context;
		Drawable? value;

		//Drawable needs ReadExternalStorage permission, if it is not allowed by the user, value is the default system wallpaper with the GetBuiltInDrawable
		try
		{
			value = WallpaperManager.GetInstance(context)?.Drawable;
		}
		catch (Java.Lang.SecurityException)
		{
			value = WallpaperManager.GetInstance(context)?.GetBuiltInDrawable(WallpaperManagerFlags.System);
		}

		if (value is BitmapDrawable bitmapDrawable)
		{
			var color = CoreApplication.RequestedTheme == SystemTheme.Dark ? DarkThemeColor : LightThemeColor;
			var opacity = CoreApplication.RequestedTheme == SystemTheme.Dark ? DarkThemeTintOpacity : LightThemeTintOpacity;

			var blurDrawable = ApplyMicaOnDrawable(bitmapDrawable, opacity, color);

			if (blurDrawable is not { })
			{
				return false;
			}

			_ = SetBackground(xamlWindow, blurDrawable);

			return true;
		}

		return false;
	}

	private BitmapDrawable? ApplyMicaOnDrawable(BitmapDrawable originalDrawable, float darkenFactor, Color color)
	{
		if (originalDrawable.Bitmap is not { } inputBitmap)
		{
			return null;
		}

		var outputBitmap = inputBitmap.Copy(inputBitmap.GetConfig(), true);

		if (outputBitmap is not { })
		{
			return null;
		}

		var canvas = new Android.Graphics.Canvas(outputBitmap);
		var paint = new Paint();

		paint.SetARGB((int)(darkenFactor * color.A), color.R, color.G, color.B);
		canvas.DrawRect(0, 0, inputBitmap.Width, inputBitmap.Height, paint);

		var _rs = RenderScript.Create(Application.Context);

		var inputAllocation = Allocation.CreateFromBitmap(_rs, outputBitmap);
		var outputAllocation = Allocation.CreateFromBitmap(_rs, outputBitmap);

		var blurScript = ScriptIntrinsicBlur.Create(_rs, Element.U8_4(_rs));

		if (blurScript is not { })
		{
			return null;
		}

		for (var i = 0; i < 120; i++)
		{
			blurScript.SetRadius(25);
			blurScript.SetInput(inputAllocation);
			blurScript.ForEach(outputAllocation);
		}

		if (outputAllocation is not { })
		{
			return null;
		}

		outputAllocation.CopyTo(outputBitmap);

		var resources = Application.Context.Resources;

		return new BitmapDrawable(resources, outputBitmap);
	}

	private async Task<bool> SetBackground(Windows.UI.Xaml.Window xamlWindow, BitmapDrawable bitmapDrawable)
	{
		if (bitmapDrawable.Bitmap is not { } bitmap)
		{
			return false;
		}

		using var stream = new MemoryStream();

		bitmap.Compress(Bitmap.CompressFormat.Png, 100, stream);
		var byteArray = stream.ToArray();

		var file = await ApplicationData.Current.LocalFolder.CreateFileAsync("background.png", CreationCollisionOption.ReplaceExisting);

		using (var fileStream = await file.OpenStreamForWriteAsync())
		{
			await fileStream.WriteAsync(byteArray, 0, byteArray.Length);
		}

		var backgroundImageBrush = new ImageBrush
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
