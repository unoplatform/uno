#nullable enable

using System;
using System.IO;
using Windows.UI;
using Microsoft.UI;
using Windows.UI.Composition;
using Windows.Graphics.Effects;
using Microsoft.Graphics.Canvas;
using System.Collections.Generic;
using Microsoft.Graphics.Canvas.Effects;

namespace Windows.UI.Xaml.Media;

public partial class AcrylicBrush
{
	private CompositionEffectBrush? _noiseBrush;
	private CompositionBrush? _brush;
	private bool _isUsingOpaqueBrush;
	private bool _isConnected;

	private const float BlurRadius = 30.0f;
	private const float NoiseOpacity = 0.02f;

	private struct EffectNames
	{
		public const string Backdrop = "Backdrop";
		public const string Blur = "Blur";
		public const string FadeInOut = "FadeInOut";
		public const string FadeInOutCrossFade = "FadeInOut.CrossFade";
		public const string Fallback = "FallbackColor";
		public const string FallbackColor = "FallbackColor.Color";
		public const string Luminosity = "LuminosityColor";
		public const string LuminosityColor = "LuminosityColor.Color";
		public const string Tint = "TintColor";
		public const string TintColor = "TintColor.Color";
		public const string Noise = "Noise";
		public const string NoiseAsset = "Uno.UI.Resources.NoiseAsset256x256.png";
		public const string NoiseSource = "NoiseSource";
		public const string NoiseOpacity = "NoiseOpacity";
	}

	protected override void OnConnected()
	{
		_isConnected = true;
		UpdateAcrylicBrush();
	}

	protected override void OnDisconnected()
	{
		_isConnected = false;

		if (_brush is not null)
		{
			_brush.Dispose();
			_brush = null;
			CompositionBrush = null;
		}

		_noiseBrush?.Dispose();
		_noiseBrush = null;
	}

	internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
		switch (args.Property.Name)
		{
			case nameof(TintColor):
			case nameof(TintOpacity):
			case nameof(TintLuminosityOpacity):
			case nameof(AlwaysUseFallback):
				UpdateAcrylicBrush();
				break;
			default:
				return;
		}
	}

	private void UpdateAcrylicBrush()
	{
		if (_isConnected)
		{
			// TODO: Currently we are force recreating the brush even if it exists because Composition animations aren't implemented yet
			CreateAcrylicBrush(useCrossFadeEffect: false, forceCreateAcrylicBrush: true);
		}
	}

	private void CreateAcrylicBrush(bool useCrossFadeEffect, bool forceCreateAcrylicBrush)
	{
		Compositor compositor = Compositor.GetSharedCompositor();

		if (forceCreateAcrylicBrush)
		{
			if
			(
#if UNO_DISABLE_ACRYLIC_ON_CPU
				compositor.IsSoftwareRenderer is true ||
#endif
				!EnsureNoiseBrush() || _noiseBrush is null
			)
			{
				CreateAcrylicBrush(false, false);
				return;
			}

			Color tintColor = GetEffectiveTintColor();
			Color luminosityColor = GetEffectiveLuminosityColor();

			_isUsingOpaqueBrush = tintColor.A == 255;

			var acrylicBrush = CreateAcrylicBrushWorker(
				compositor,
				false,
				useCrossFadeEffect,
				tintColor,
				luminosityColor,
				FallbackColor,
				_isUsingOpaqueBrush);

			if (acrylicBrush is null)
			{
				CreateAcrylicBrush(false, false);
				return;
			}

			// Set noise image source
			acrylicBrush.SetSourceParameter("Noise", _noiseBrush);

			// Expand the blur region
			acrylicBrush.UseBlurPadding = true;

			// TODO: Composition properties aren't supported yet
			/*acrylicBrush.Properties.InsertColor("TintColor.Color", tintColor);
			if (!_isUsingOpaqueBrush)
			{
				acrylicBrush.Properties.InsertColor("LuminosityColor.Color", luminosityColor);
			}

			if (useCrossFadeEffect)
			{
				acrylicBrush.Properties.InsertColor("FallbackColor.Color", FallbackColor);
			}*/

			// Update the AcrylicBrush
			_brush = acrylicBrush;
		}
		else
		{
			_brush = compositor.CreateColorBrush(FallbackColor);
		}

		CompositionBrush = _brush;
	}

	private bool EnsureNoiseBrush()
	{
		if (_noiseBrush is null)
		{
			Compositor compositor = Compositor.GetSharedCompositor();
			CompositionSurfaceBrush surfaceBrush = compositor.CreateSurfaceBrush();
			SkiaCompositionSurface surface = new SkiaCompositionSurface();
			using Stream? imgStream = GetType().Assembly.GetManifestResourceStream(EffectNames.NoiseAsset);

			if (imgStream is not null && surface.LoadFromStream(256, 256, imgStream).success)
			{
				surfaceBrush.Surface = surface;
				surfaceBrush.Stretch = CompositionStretch.None;

				var borderEffect = new BorderEffect() { Source = new CompositionEffectSourceParameter(EffectNames.NoiseSource), ExtendX = CanvasEdgeBehavior.Wrap, ExtendY = CanvasEdgeBehavior.Wrap };
				var effectFactory = compositor.CreateEffectFactory(borderEffect);
				_noiseBrush = effectFactory.CreateBrush();

				if (_noiseBrush is not null)
				{
					_noiseBrush.SetSourceParameter(EffectNames.NoiseSource, surfaceBrush);
					return true;
				}
			}

			surfaceBrush.Dispose();
			return false;
		}

		return true;
	}

	private CompositionEffectBrush? CreateAcrylicBrushWorker(Compositor compositor, bool useWindowAcrylic, bool useCrossFadeEffect, Color initialTintColor, Color initialLuminosityColor, Color initialFallbackColor, bool shouldBrushBeOpaque)
	{

		var effectFactory = CreateAcrylicBrushCompositionEffectFactory(
			compositor, shouldBrushBeOpaque, useWindowAcrylic, useCrossFadeEffect,
			initialTintColor, initialLuminosityColor, initialFallbackColor);

		// Create the Comp effect Brush
		CompositionEffectBrush? acrylicBrush = effectFactory.CreateBrush();

		// Set the backdrop source
		if (!shouldBrushBeOpaque)
		{
			if (useWindowAcrylic)
			{
				//var hostBackdropBrush = compositor.CreateHostBackdropBrush();
				var hostBackdropBrush = compositor.CreateBackdropBrush(); // We don't have HostBackdropBrush support yet
				acrylicBrush?.SetSourceParameter(EffectNames.Backdrop, hostBackdropBrush);
			}
			else
			{
				var backdropBrush = compositor.CreateBackdropBrush();
				acrylicBrush?.SetSourceParameter(EffectNames.Backdrop, backdropBrush);
			}
		}

		return acrylicBrush;
	}

	private CompositionEffectFactory CreateAcrylicBrushCompositionEffectFactory(Compositor compositor, bool shouldBrushBeOpaque, bool useWindowAcrylic, bool useCrossFadeEffect, Color initialTintColor, Color initialLuminosityColor, Color initialFallbackColor)
	{
		CompositionEffectFactory? effectFactory = null;

		// The part of the effect graph below the noise layer. This is either a semi-transparent tint (common) or an opaque tint (uncommon).
		// Opaque tint may be used by apps wishing add the complexity of noise to their brand color, for example.
		IGraphicsEffect tintOutput;

		// Tint Color - either used directly or in a Color blend over a blurred backdrop
		var tintColorEffect = new ColorSourceEffect();
		tintColorEffect.Name = EffectNames.Tint;
		tintColorEffect.Color = initialTintColor;

		List<string> animatedProperties = new() { EffectNames.TintColor };

		if (shouldBrushBeOpaque)
		{
			tintOutput = tintColorEffect;
		}

		else
		{
			// Load the backdrop in a brush
			CompositionEffectSourceParameter backdropEffectSourceParameter = new(EffectNames.Backdrop);

			// Get a blurred backdrop...
			IGraphicsEffectSource blurredSource;
			if (useWindowAcrylic)
			{
				// ...either the shell baked the blur into the backdrop brush, and we use it directly...
				blurredSource = backdropEffectSourceParameter;
			}
			else
			{
				// ...or we apply the blur ourselves
				var gaussianBlurEffect = new GaussianBlurEffect();
				gaussianBlurEffect.Name = EffectNames.Blur;
				gaussianBlurEffect.BorderMode = EffectBorderMode.Hard;
				gaussianBlurEffect.BlurAmount = BlurRadius;
				gaussianBlurEffect.Source = backdropEffectSourceParameter;
				blurredSource = gaussianBlurEffect;
			}

			tintOutput = CombineNoiseWithTintEffect(blurredSource, tintColorEffect, initialLuminosityColor, animatedProperties);
		}

		// Create noise with alpha:
		CompositionEffectSourceParameter noiseEffectSourceParameter = new(EffectNames.Noise);
		var noiseOpacityEffect = new OpacityEffect();
		noiseOpacityEffect.Name = EffectNames.NoiseOpacity;
		noiseOpacityEffect.Opacity = NoiseOpacity;
		noiseOpacityEffect.Source = noiseEffectSourceParameter;

		// Blend noise on top of tint
		var blendEffectOuter = new CompositeEffect();
		blendEffectOuter.Mode = CanvasComposite.SourceOver;
		blendEffectOuter.Sources.Add(tintOutput);
		blendEffectOuter.Sources.Add(noiseOpacityEffect);

		if (useCrossFadeEffect)
		{
			// Fallback color
			var fallbackColorEffect = new ColorSourceEffect();
			fallbackColorEffect.Name = EffectNames.Fallback;
			fallbackColorEffect.Color = initialFallbackColor;

			// CrossFade with the fallback color. CrossFade = 0 means full fallback, 1 means full acrylic.
			var fadeInOutEffect = new CrossFadeEffect();
			fadeInOutEffect.Name = EffectNames.FadeInOut;
			fadeInOutEffect.Source1 = fallbackColorEffect;
			fadeInOutEffect.Source2 = blendEffectOuter;
			fadeInOutEffect.CrossFade = 1.0f;

			animatedProperties.Add(EffectNames.FallbackColor);
			animatedProperties.Add(EffectNames.FadeInOutCrossFade);
			effectFactory = compositor.CreateEffectFactory(fadeInOutEffect, animatedProperties);
		}
		else
		{
			effectFactory = compositor.CreateEffectFactory(blendEffectOuter, animatedProperties);
		}

		return effectFactory;
	}

	private IGraphicsEffect CombineNoiseWithTintEffect(IGraphicsEffectSource blurredSource, ColorSourceEffect tintColorEffect, Color initialLuminosityColor, IList<string>? animatedProperties = null)
	{
		animatedProperties?.Add(EffectNames.LuminosityColor);

		// Apply luminosity:

		// Luminosity Color
		var luminosityColorEffect = new ColorSourceEffect();
		luminosityColorEffect.Name = EffectNames.Luminosity;
		luminosityColorEffect.Color = initialLuminosityColor;

		// Luminosity blend
		var luminosityBlendEffect = new BlendEffect();
		// NOTE: There is currently a bug in Windows where the names of BlendEffectMode.Luminosity and BlendEffectMode.Color are flipped.
		// This should be changed to Luminosity when/if the bug is fixed.
		luminosityBlendEffect.Mode = BlendEffectMode.Color;
		luminosityBlendEffect.Background = blurredSource;
		luminosityBlendEffect.Foreground = luminosityColorEffect;

		// Apply tint:

		// Color blend
		var colorBlendEffect = new BlendEffect();
		// NOTE: There is currently a bug in Windows where the names of BlendEffectMode.Luminosity and BlendEffectMode.Color are flipped.
		// This should be changed to Color when/if the bug is fixed.
		colorBlendEffect.Mode = BlendEffectMode.Luminosity;
		colorBlendEffect.Background = luminosityBlendEffect;
		colorBlendEffect.Foreground = tintColorEffect;

		return colorBlendEffect;
	}

	private Color GetEffectiveLuminosityColor()
	{
		Color tintColor = TintColor;

		// Purposely leaving out tint opacity modifier here because GetLuminosityColor needs the *original* tint opacity set by the user.
		tintColor.A = (byte)Math.Round(tintColor.A * TintOpacity);

		return GetLuminosityColor(tintColor, TintLuminosityOpacity);
	}

	private Color GetLuminosityColor(Color tintColor, double? luminosityOpacity)
	{
		// If luminosity opacity is specified, just use the values as is
		if (luminosityOpacity is not null)
		{
			return tintColor with { A = (byte)(Math.Clamp(luminosityOpacity.Value, 0.0, 1.0) * 255.0f) };
		}
		else
		{
			// To create the Luminosity blend input color without luminosity opacity,
			// we're taking the TintColor input, converting to HSV, and clamping the V between these values
			const double minHsvV = 0.125;
			const double maxHsvV = 0.965;

			Hsv hsvTintColor = RgbToHsv(tintColor);

			var clampedHsvV = Math.Clamp(hsvTintColor.V, minHsvV, maxHsvV);

			Hsv hsvLuminosityColor = new Hsv(hsvTintColor.H, hsvTintColor.S, clampedHsvV);
			Rgb rgbLuminosityColor = HsvToRgb(hsvLuminosityColor);

			// Now figure out luminosity opacity
			// Map original *tint* opacity to this range
			const double minLuminosityOpacity = 0.15;
			const double maxLuminosityOpacity = 1.03;

			double luminosityOpacityRangeMax = maxLuminosityOpacity - minLuminosityOpacity;
			double mappedTintOpacity = ((tintColor.A / 255.0) * luminosityOpacityRangeMax) + minLuminosityOpacity;

			// Finally, combine the luminosity opacity and the HsvV-clamped tint color
			return ((Color)rgbLuminosityColor) with { A = (byte)(Math.Min(mappedTintOpacity, 1.0) * 255.0f) };
		}

	}

	private Color GetEffectiveTintColor()
	{
		Color tintColor = TintColor;

		// Update tintColor's alpha with the combined opacity value
		// If LuminosityOpacity was specified, we don't intervene into users parameters
		if (TintLuminosityOpacity is not null)
		{
			tintColor.A = (byte)Math.Round(tintColor.A * TintOpacity);
		}
		else
		{
			double tintOpacityModifier = GetTintOpacityModifier(tintColor);
			tintColor.A = (byte)Math.Round(tintColor.A * TintOpacity * tintOpacityModifier);
		}

		return tintColor;
	}

	private double GetTintOpacityModifier(Color tintColor)
	{
		const double midPoint = 0.50;

		const double whiteMaxOpacity = 0.45;
		const double midPointMaxOpacity = 0.90;
		const double blackMaxOpacity = 0.85;

		Hsv hsv = RgbToHsv(tintColor);

		double opacityModifier = midPointMaxOpacity;

		if (hsv.V != midPoint)
		{
			// Determine maximum suppression amount
			double lowestMaxOpacity = midPointMaxOpacity;
			double maxDeviation = midPoint;

			if (hsv.V > midPoint)
			{
				lowestMaxOpacity = whiteMaxOpacity; // At white (100% hsvV)
				maxDeviation = 1 - maxDeviation;
			}
			else if (hsv.V < midPoint)
			{
				lowestMaxOpacity = blackMaxOpacity; // At black (0% hsvV)
			}

			double maxOpacitySuppression = midPointMaxOpacity - lowestMaxOpacity;

			// Determine normalized deviation from the midpoint
			double deviation = Math.Abs(hsv.V - midPoint);
			double normalizedDeviation = deviation / maxDeviation;

			// If we have saturation, reduce opacity suppression to allow that color to come through more
			if (hsv.S > 0)
			{
				// Dampen opacity suppression based on how much saturation there is
				maxOpacitySuppression *= Math.Max(1 - (hsv.S * 2), 0.0);
			}

			double opacitySuppression = maxOpacitySuppression * normalizedDeviation;

			opacityModifier = midPointMaxOpacity - opacitySuppression;
		}

		return opacityModifier;
	}

	#region ColorConversion
	Hsv RgbToHsv(Rgb rgb)
	{
		double hue = 0;
		double saturation = 0;
		double value = 0;

		double max = rgb.R >= rgb.G ? (rgb.R >= rgb.B ? rgb.R : rgb.B) : (rgb.G >= rgb.B ? rgb.G : rgb.B);
		double min = rgb.R <= rgb.G ? (rgb.R <= rgb.B ? rgb.R : rgb.B) : (rgb.G <= rgb.B ? rgb.G : rgb.B);
		value = max;

		double chroma = max - min;

		if (chroma == 0)
		{
			hue = 0.0;
			saturation = 0.0;
		}
		else
		{
			if (rgb.R == max)
			{
				hue = 60 * (rgb.G - rgb.B) / chroma;
			}
			else if (rgb.G == max)
			{
				hue = 120 + 60 * (rgb.B - rgb.R) / chroma;
			}
			else
			{
				hue = 240 + 60 * (rgb.R - rgb.G) / chroma;
			}

			if (hue < 0.0)
			{
				hue += 360.0;
			}

			saturation = chroma / value;
		}

		return new Hsv(hue, saturation, value);
	}

	Rgb HsvToRgb(Hsv hsv)
	{
		double hue = hsv.H;
		double saturation = hsv.S;
		double value = hsv.V;

		while (hue >= 360.0)
		{
			hue -= 360.0;
		}

		while (hue < 0.0)
		{
			hue += 360.0;
		}

		saturation = saturation < 0.0 ? 0.0 : saturation;
		saturation = saturation > 1.0 ? 1.0 : saturation;

		value = value < 0.0 ? 0.0 : value;
		value = value > 1.0 ? 1.0 : value;

		double chroma = saturation * value;
		double min = value - chroma;

		if (chroma == 0)
		{
			return new Rgb(min, min, min);
		}

		int sextant = (int)(hue / 60d);
		double intermediateColorPercentage = hue / 60d - sextant;
		double max = chroma + min;

		double r = 0;
		double g = 0;
		double b = 0;

		switch (sextant)
		{
			case 0:
				r = max;
				g = min + chroma * intermediateColorPercentage;
				b = min;
				break;
			case 1:
				r = min + chroma * (1 - intermediateColorPercentage);
				g = max;
				b = min;
				break;
			case 2:
				r = min;
				g = max;
				b = min + chroma * intermediateColorPercentage;
				break;
			case 3:
				r = min;
				g = min + chroma * (1 - intermediateColorPercentage);
				b = max;
				break;
			case 4:
				r = min + chroma * intermediateColorPercentage;
				g = min;
				b = max;
				break;
			case 5:
				r = max;
				g = min;
				b = min + chroma * (1 - intermediateColorPercentage);
				break;
		}

		return new Rgb(r, g, b);
	}

	private struct Rgb
	{
		public double R;
		public double G;
		public double B;

		public Rgb(double r, double g, double b)
		{
			R = r;
			G = g;
			B = b;
		}

		public static implicit operator Rgb(Color color) => new(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f);
		public static implicit operator Color(Rgb color) => new(255, (byte)(color.R * 255.0f), (byte)(color.G * 255.0f), (byte)(color.B * 255.0f));
	}

	private struct Hsv
	{
		public double H;
		public double S;
		public double V;

		public Hsv(double h, double s, double v)
		{
			H = h;
			S = s;
			V = v;
		}
	}
	#endregion
}
