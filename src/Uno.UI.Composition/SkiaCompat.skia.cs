#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;

namespace Uno.UI.Composition;

internal static class SkiaCompat
{
	private static readonly bool _isSkiaSharp3OrLater = typeof(SKPaint).Assembly.GetName().Version?.Major >= 3;

	internal static SKImageFilter SKImageFilter_CreateBlur(float sigmaX, float sigmaY, SKImageFilter? input, SKRect cropRect)
	{
		if (!_isSkiaSharp3OrLater)
		{
			[MethodImpl(MethodImplOptions.NoInlining)]
			SKImageFilter Legacy() => SKImageFilter.CreateBlur(sigmaX, sigmaY, input, new SKImageFilter.CropRect(cropRect));
			return Legacy();
		}

#if NET8_0_OR_GREATER
		return SKImageFilter_CreateBlur(null, sigmaX, sigmaY, input, cropRect);
#else
		throw new NotSupportedException("SkiaSharp 3 is only supported for .NET 8 and later.");
#endif
	}

#if NET8_0_OR_GREATER
	[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "CreateBlur")]
	private static extern SKImageFilter SKImageFilter_CreateBlur(SKImageFilter? _, float sigmaX, float sigmaY, SKImageFilter? input, SKRect cropRect);
#endif

	internal static SKImageFilter SKImageFilter_CreateColorFilter(SKColorFilter cf, SKImageFilter? input, SKRect cropRect)
	{
		if (!_isSkiaSharp3OrLater)
		{
			[MethodImpl(MethodImplOptions.NoInlining)]
			SKImageFilter Legacy() => SKImageFilter.CreateColorFilter(cf, input, new SKImageFilter.CropRect(cropRect));
			return Legacy();
		}

#if NET8_0_OR_GREATER
		return SKImageFilter_CreateColorFilter(null, cf, input, cropRect);
#else
		throw new NotSupportedException("SkiaSharp 3 is only supported for .NET 8 and later.");
#endif
	}

#if NET8_0_OR_GREATER
	[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "CreateColorFilter")]
	private static extern SKImageFilter SKImageFilter_CreateColorFilter(SKImageFilter? _, SKColorFilter cf, SKImageFilter? input, SKRect cropRect);
#endif

	internal static SKImageFilter SKImageFilter_CreateColorFilter(SKColorFilter cf, SKImageFilter? input)
	{
		if (!_isSkiaSharp3OrLater)
		{
			[MethodImpl(MethodImplOptions.NoInlining)]
			SKImageFilter Legacy() => SKImageFilter.CreateColorFilter(cf, input);
			return Legacy();
		}

#if NET8_0_OR_GREATER
		return SKImageFilter_CreateColorFilter(null, cf, input);
#else
		throw new NotSupportedException("SkiaSharp 3 is only supported for .NET 8 and later.");
#endif
	}

#if NET8_0_OR_GREATER
	[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "CreateColorFilter")]
	private static extern SKImageFilter SKImageFilter_CreateColorFilter(SKImageFilter? _, SKColorFilter cf, SKImageFilter? input);
#endif

	internal static SKImageFilter SKImageFilter_CreateOffset(float dx, float dy, SKImageFilter? input, SKRect cropRect)
	{
		if (!_isSkiaSharp3OrLater)
		{
			[MethodImpl(MethodImplOptions.NoInlining)]
			SKImageFilter Legacy() => SKImageFilter.CreateOffset(dx, dy, input, new SKImageFilter.CropRect(cropRect));
			return Legacy();
		}

#if NET8_0_OR_GREATER
		return SKImageFilter_CreateOffset(null, dx, dy, input, cropRect);
#else
		throw new NotSupportedException("SkiaSharp 3 is only supported for .NET 8 and later.");
#endif
	}

#if NET8_0_OR_GREATER
	[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "CreateOffset")]
	private static extern SKImageFilter SKImageFilter_CreateOffset(SKImageFilter? _, float radiusX, float radiusY, SKImageFilter? input, SKRect cropRect);
#endif

	internal static SKRuntimeEffect SKRuntimeEffect_CreateShader(string sksl, out string errors)
	{
		if (!_isSkiaSharp3OrLater)
		{
			[MethodImpl(MethodImplOptions.NoInlining)]
			SKRuntimeEffect Legacy(out string errors) => SKRuntimeEffect.Create(sksl, out errors);
			return Legacy(out errors);
		}

#if NET8_0_OR_GREATER
		return SKRuntimeEffect_CreateShader(null, sksl, out errors);
#else
		throw new NotSupportedException("SkiaSharp 3 is only supported for .NET 8 and later.");
#endif
	}

#if NET8_0_OR_GREATER
	[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "CreateShader")]
	private static extern SKRuntimeEffect SKRuntimeEffect_CreateShader(SKRuntimeEffect? _, string sksl, out string errors);
#endif

	internal static void SKRuntimeEffectChildren_Add_WithNull(SKRuntimeEffectChildren @this, string name)
	{
		if (!_isSkiaSharp3OrLater)
		{
			[MethodImpl(MethodImplOptions.NoInlining)]
			void Legacy() => @this.Add(name, null);
			Legacy();
			return;
		}

#if NET8_0_OR_GREATER
		// TODO(Youssef,SkiaSharp3): Find a way to support this... Reflection?
		throw new NotSupportedException("CompositionEffectBrush is not supported for SkiaSharp 3.");
#else
		throw new NotSupportedException("SkiaSharp 3 is only supported for .NET 8 and later.");
#endif
	}

	internal static SKImageFilter SKImageFilter_CreateBlendMode(SKBlendMode mode, SKImageFilter? background, SKImageFilter? foreground, SKRect cropRect)
	{
		if (!_isSkiaSharp3OrLater)
		{
			[MethodImpl(MethodImplOptions.NoInlining)]
			SKImageFilter Legacy() => SKImageFilter.CreateBlendMode(mode, background, foreground, new SKImageFilter.CropRect(cropRect));
			return Legacy();
		}

#if NET8_0_OR_GREATER
		return SKImageFilter_CreateBlendMode(null, mode, background, foreground, cropRect);
#else
		throw new NotSupportedException("SkiaSharp 3 is only supported for .NET 8 and later.");
#endif
	}

#if NET8_0_OR_GREATER
	[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "CreateBlendMode")]
	private static extern SKImageFilter SKImageFilter_CreateBlendMode(SKImageFilter? _, SKBlendMode mode, SKImageFilter? background, SKImageFilter? foreground, SKRect cropRect);
#endif

	internal static SKImageFilter SKImageFilter_CreatePaint(SKPaint paint, SKRect cropRect)
	{
		if (!_isSkiaSharp3OrLater)
		{
			[MethodImpl(MethodImplOptions.NoInlining)]
			SKImageFilter Legacy() => SKImageFilter.CreatePaint(paint, new SKImageFilter.CropRect(cropRect));
			return Legacy();
		}

#if NET8_0_OR_GREATER
		return SKImageFilter_CreatePaint(null, paint, cropRect);
#else
		throw new NotSupportedException("SkiaSharp 3 is only supported for .NET 8 and later.");
#endif
	}

#if NET8_0_OR_GREATER
	[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "CreatePaint")]
	private static extern SKImageFilter SKImageFilter_CreatePaint(SKImageFilter? _, SKPaint paint, SKRect cropRect);
#endif

	internal static SKImageFilter SKImageFilter_CreateDropShadow(float dx, float dy, float sigmaX, float sigmaY, SKColor color)
	{
		if (!_isSkiaSharp3OrLater)
		{
			[MethodImpl(MethodImplOptions.NoInlining)]
			SKImageFilter Legacy() => SKImageFilter.CreateDropShadow(dx, dy, sigmaX, sigmaY, color);
			return Legacy();
		}

#if NET8_0_OR_GREATER
		return SKImageFilter_CreateDropShadow(null, dx, dy, sigmaX, sigmaY, color);
#else
		throw new NotSupportedException("SkiaSharp 3 is only supported for .NET 8 and later.");
#endif
	}

#if NET8_0_OR_GREATER
	[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "CreateDropShadow")]
	private static extern SKImageFilter SKImageFilter_CreateDropShadow(SKImageFilter? _, float dx, float dy, float sigmaX, float sigmaY, SKColor color);
#endif

	internal static SKImageFilter SKImageFilter_CreateArithmetic(float k1, float k2, float k3, float k4, bool enforcePMColor, SKImageFilter? background, SKImageFilter? foreground, SKRect cropRect)
	{
		if (!_isSkiaSharp3OrLater)
		{
			[MethodImpl(MethodImplOptions.NoInlining)]
			SKImageFilter Legacy() => SKImageFilter.CreateArithmetic(k1, k2, k3, k4, enforcePMColor, background, foreground, new SKImageFilter.CropRect(cropRect));
			return Legacy();
		}

#if NET8_0_OR_GREATER
		return SKImageFilter_CreateArithmetic(null, k1, k2, k3, k4, enforcePMColor, background, foreground, cropRect);
#else
		throw new NotSupportedException("SkiaSharp 3 is only supported for .NET 8 and later.");
#endif
	}

#if NET8_0_OR_GREATER
	[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "CreateArithmetic")]
	private static extern SKImageFilter SKImageFilter_CreateArithmetic(SKImageFilter? _, float k1, float k2, float k3, float k4, bool enforcePMColor, SKImageFilter? background, SKImageFilter? foreground, SKRect cropRect);
#endif

	internal static SKImageFilter SKImageFilter_CreateMerge(ReadOnlySpan<SKImageFilter> filters, SKRect cropRect)
	{
		if (!_isSkiaSharp3OrLater)
		{
			[MethodImpl(MethodImplOptions.NoInlining)]
			SKImageFilter Legacy(ReadOnlySpan<SKImageFilter> filters) => SKImageFilter.CreateMerge(filters.ToArray(), new SKImageFilter.CropRect(cropRect));
			return Legacy(filters);
		}

#if NET8_0_OR_GREATER
		return SKImageFilter_CreateMerge(null, filters, cropRect);
#else
		throw new NotSupportedException("SkiaSharp 3 is only supported for .NET 8 and later.");
#endif
	}

#if NET8_0_OR_GREATER
	[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "CreateMerge")]
	private static extern SKImageFilter SKImageFilter_CreateMerge(SKImageFilter? _, ReadOnlySpan<SKImageFilter> filters, SKRect cropRect);
#endif

	internal static SKImageFilter SKImageFilter_CreateMatrixConvolution(SKSizeI kernelSize, ReadOnlySpan<float> kernel, float gain, float bias, SKPointI kernelOffset, SKShaderTileMode tileMode, bool convolveAlpha, SKImageFilter? input, SKRect cropRect)
	{
		if (!_isSkiaSharp3OrLater)
		{
			[MethodImpl(MethodImplOptions.NoInlining)]
			SKImageFilter Legacy(ReadOnlySpan<float> kernel) => SKImageFilter.CreateMatrixConvolution(kernelSize, kernel.ToArray(), gain, bias, kernelOffset, tileMode, convolveAlpha, input, new SKImageFilter.CropRect(cropRect));
			return Legacy(kernel);
		}

#if NET8_0_OR_GREATER
		return SKImageFilter_CreateMatrixConvolution(null, kernelSize, kernel, gain, bias, kernelOffset, tileMode, convolveAlpha, input, cropRect);
#else
		throw new NotSupportedException("SkiaSharp 3 is only supported for .NET 8 and later.");
#endif
	}

#if NET8_0_OR_GREATER
	[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "CreateMatrixConvolution")]
	private static extern SKImageFilter SKImageFilter_CreateMatrixConvolution(SKImageFilter? _, SKSizeI kernelSize, ReadOnlySpan<float> kernel, float gain, float bias, SKPointI kernelOffset, SKShaderTileMode tileMode, bool convolveAlpha, SKImageFilter? input, SKRect cropRect);
#endif

	internal static SKImageFilter SKImageFilter_CreateDistantLitDiffuse(SKPoint3 direction, SKColor lightColor, float surfaceScale, float kd, SKImageFilter? input, SKRect cropRect)
	{
		if (!_isSkiaSharp3OrLater)
		{
			[MethodImpl(MethodImplOptions.NoInlining)]
			SKImageFilter Legacy() => SKImageFilter.CreateDistantLitDiffuse(direction, lightColor, surfaceScale, kd, input, new SKImageFilter.CropRect(cropRect));
			return Legacy();
		}

#if NET8_0_OR_GREATER
		return SKImageFilter_CreateDistantLitDiffuse(null, direction, lightColor, surfaceScale, kd, input, cropRect);
#else
		throw new NotSupportedException("SkiaSharp 3 is only supported for .NET 8 and later.");
#endif
	}

#if NET8_0_OR_GREATER
	[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "CreateDistantLitDiffuse")]
	private static extern SKImageFilter SKImageFilter_CreateDistantLitDiffuse(SKImageFilter? _, SKPoint3 direction, SKColor lightColor, float surfaceScale, float kd, SKImageFilter? input, SKRect cropRect);
#endif

	internal static SKImageFilter SKImageFilter_CreateDistantLitSpecular(SKPoint3 direction, SKColor lightColor, float surfaceScale, float ks, float shininess, SKImageFilter? input, SKRect cropRect)
	{
		if (!_isSkiaSharp3OrLater)
		{
			[MethodImpl(MethodImplOptions.NoInlining)]
			SKImageFilter Legacy() => SKImageFilter.CreateDistantLitSpecular(direction, lightColor, surfaceScale, ks, shininess, input, new SKImageFilter.CropRect(cropRect));
			return Legacy();
		}

#if NET8_0_OR_GREATER
		return SKImageFilter_CreateDistantLitSpecular(null, direction, lightColor, surfaceScale, ks, shininess, input, cropRect);
#else
		throw new NotSupportedException("SkiaSharp 3 is only supported for .NET 8 and later.");
#endif
	}

#if NET8_0_OR_GREATER
	[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "CreateDistantLitSpecular")]
	private static extern SKImageFilter SKImageFilter_CreateDistantLitSpecular(SKImageFilter? _, SKPoint3 direction, SKColor lightColor, float surfaceScale, float ks, float shininess, SKImageFilter? input, SKRect cropRect);
#endif

	internal static SKImageFilter SKImageFilter_CreateSpotLitDiffuse(SKPoint3 location, SKPoint3 target, float specularExponent, float cutoffAngle, SKColor lightColor, float surfaceScale, float kd, SKImageFilter? input, SKRect cropRect)
	{
		if (!_isSkiaSharp3OrLater)
		{
			[MethodImpl(MethodImplOptions.NoInlining)]
			SKImageFilter Legacy() => SKImageFilter.CreateSpotLitDiffuse(location, target, specularExponent, cutoffAngle, lightColor, surfaceScale, kd, input, new SKImageFilter.CropRect(cropRect));
			return Legacy();
		}

#if NET8_0_OR_GREATER
		return SKImageFilter_CreateSpotLitDiffuse(null, location, target, specularExponent, cutoffAngle, lightColor, surfaceScale, kd, input, cropRect);
#else
		throw new NotSupportedException("SkiaSharp 3 is only supported for .NET 8 and later.");
#endif
	}

#if NET8_0_OR_GREATER
	[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "CreateSpotLitDiffuse")]
	private static extern SKImageFilter SKImageFilter_CreateSpotLitDiffuse(SKImageFilter? _, SKPoint3 location, SKPoint3 target, float specularExponent, float cutoffAngle, SKColor lightColor, float surfaceScale, float kd, SKImageFilter? input, SKRect cropRect);
#endif

	internal static SKImageFilter SKImageFilter_CreateSpotLitSpecular(SKPoint3 location, SKPoint3 target, float specularExponent, float cutoffAngle, SKColor lightColor, float surfaceScale, float ks, float shininess, SKImageFilter? input, SKRect cropRect)
	{
		if (!_isSkiaSharp3OrLater)
		{
			[MethodImpl(MethodImplOptions.NoInlining)]
			SKImageFilter Legacy() => SKImageFilter.CreateSpotLitSpecular(location, target, specularExponent, cutoffAngle, lightColor, surfaceScale, ks, shininess, input, new SKImageFilter.CropRect(cropRect));
			return Legacy();
		}

#if NET8_0_OR_GREATER
		return SKImageFilter_CreateSpotLitSpecular(null, location, target, specularExponent, cutoffAngle, lightColor, surfaceScale, ks, shininess, input, cropRect);
#else
		throw new NotSupportedException("SkiaSharp 3 is only supported for .NET 8 and later.");
#endif
	}

#if NET8_0_OR_GREATER
	[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "CreateSpotLitSpecular")]
	private static extern SKImageFilter SKImageFilter_CreateSpotLitSpecular(SKImageFilter? _, SKPoint3 location, SKPoint3 target, float specularExponent, float cutoffAngle, SKColor lightColor, float surfaceScale, float ks, float shininess, SKImageFilter? input, SKRect cropRect);
#endif

	internal static SKImageFilter SKImageFilter_CreatePointLitDiffuse(SKPoint3 location, SKColor lightColor, float surfaceScale, float kd, SKImageFilter? input, SKRect cropRect)
	{
		if (!_isSkiaSharp3OrLater)
		{
			[MethodImpl(MethodImplOptions.NoInlining)]
			SKImageFilter Legacy() => SKImageFilter.CreatePointLitDiffuse(location, lightColor, surfaceScale, kd, input, new SKImageFilter.CropRect(cropRect));
			return Legacy();
		}

#if NET8_0_OR_GREATER
		return SKImageFilter_CreatePointLitDiffuse(null, location, lightColor, surfaceScale, kd, input, cropRect);
#else
		throw new NotSupportedException("SkiaSharp 3 is only supported for .NET 8 and later.");
#endif
	}

#if NET8_0_OR_GREATER
	[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "CreatePointLitDiffuse")]
	private static extern SKImageFilter SKImageFilter_CreatePointLitDiffuse(SKImageFilter? _, SKPoint3 location, SKColor lightColor, float surfaceScale, float kd, SKImageFilter? input, SKRect cropRect);
#endif

	internal static SKImageFilter SKImageFilter_CreatePointLitSpecular(SKPoint3 location, SKColor lightColor, float surfaceScale, float ks, float shininess, SKImageFilter? input, SKRect cropRect)
	{
		if (!_isSkiaSharp3OrLater)
		{
			[MethodImpl(MethodImplOptions.NoInlining)]
			SKImageFilter Legacy() => SKImageFilter.CreatePointLitSpecular(location, lightColor, surfaceScale, ks, shininess, input, new SKImageFilter.CropRect(cropRect));
			return Legacy();
		}

#if NET8_0_OR_GREATER
		return SKImageFilter_CreatePointLitSpecular(null, location, lightColor, surfaceScale, ks, shininess, input, cropRect);
#else
		throw new NotSupportedException("SkiaSharp 3 is only supported for .NET 8 and later.");
#endif
	}

#if NET8_0_OR_GREATER
	[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "CreatePointLitSpecular")]
	private static extern SKImageFilter SKImageFilter_CreatePointLitSpecular(SKImageFilter? _, SKPoint3 location, SKColor lightColor, float surfaceScale, float ks, float shininess, SKImageFilter? input, SKRect cropRect);
#endif

	internal static SKShader SKRuntimeEffect_ToShader(SKRuntimeEffect @this, bool isOpaque, SKRuntimeEffectUniforms uniforms)
	{
		if (!_isSkiaSharp3OrLater)
		{
			[MethodImpl(MethodImplOptions.NoInlining)]
			SKShader Legacy() => @this.ToShader(isOpaque, uniforms);
			return Legacy();
		}

#if NET8_0_OR_GREATER
		// Note: No overload of ToShader in SkiaSharp 3 takes isOpaque parameter. So we ignore.
		return SKRuntimeEffect_ToShader(@this, uniforms);
#else
		throw new NotSupportedException("SkiaSharp 3 is only supported for .NET 8 and later.");
#endif
	}

#if NET8_0_OR_GREATER
	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "ToShader")]
	private static extern SKShader SKRuntimeEffect_ToShader(SKRuntimeEffect @this, SKRuntimeEffectUniforms uniforms);
#endif

	internal static ReadOnlySpan<byte> SKBitmap_GetPixelSpan(SKBitmap @this)
	{
		if (!_isSkiaSharp3OrLater)
		{
			[MethodImpl(MethodImplOptions.NoInlining)]
			ReadOnlySpan<byte> Legacy() => @this.GetPixelSpan();
			return Legacy();
		}

#if NET8_0_OR_GREATER
		return SKBitmap_GetPixelSpan_SkiaSharp(@this);
#else
		throw new NotSupportedException("SkiaSharp 3 is only supported for .NET 8 and later.");
#endif
	}

#if NET8_0_OR_GREATER
	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "GetPixelSpan")]
	private static extern Span<byte> SKBitmap_GetPixelSpan_SkiaSharp(SKBitmap @this);
#endif
}
