using Windows.Graphics.Effects;
using Windows.Graphics.Effects.Interop;
using System;
using SkiaSharp;
using System.Reflection.Emit;

namespace Windows.UI.Composition
{
	public partial class CompositionEffectBrush : CompositionBrush
	{
		private SKImageFilter GenerateEffectFilter(object effect, SKRect bounds)
		{
			// TODO: https://user-images.githubusercontent.com/34550324/264485558-d7ee5062-b0e0-4f6e-a8c7-0620ec561d3d.png
			// TODO: Cache pixel shaders (see dwmcore.dll!CCompiledEffectCache)

			switch (effect)
			{
				case CompositionEffectSourceParameter effectSourceParameter:
					{
						CompositionBrush brush = GetSourceParameter(effectSourceParameter.Name);
						if (brush is not null)
						{
							SKPaint paint = new SKPaint() { IsAntialias = true, IsAutohinted = true, FilterQuality = SKFilterQuality.High };
							brush.UpdatePaint(paint, bounds);

							return SKImageFilter.CreatePaint(paint, new(bounds));
						}

						return null;
					}
				case IGraphicsEffectD2D1Interop effectInterop:
					{
						switch (EffectHelpers.GetEffectType(effectInterop.GetEffectId()))
						{
							case EffectType.GaussianBlurEffect:
								{
									if (effectInterop.GetSourceCount() == 1 && effectInterop.GetPropertyCount() == 3 && effectInterop.GetSource(0) is IGraphicsEffectSource source)
									{
										SKImageFilter sourceFilter = GenerateEffectFilter(source, bounds);
										if (sourceFilter is null)
											return null;

										effectInterop.GetNamedPropertyMapping("BlurAmount", out uint sigmaProp, out _);
										effectInterop.GetNamedPropertyMapping("Optimization", out uint optProp, out _);
										effectInterop.GetNamedPropertyMapping("BorderMode", out uint borderProp, out _);

										// TODO: Implement support for other GraphicsEffectPropertyMapping values than Direct
										float sigma = (float)effectInterop.GetProperty(sigmaProp);
										_ = (uint)effectInterop.GetProperty(optProp); // TODO
										_ = (uint)effectInterop.GetProperty(borderProp); // TODO

										return SKImageFilter.CreateBlur(sigma, sigma, sourceFilter, new(bounds));
									}

									return null;
								}
							case EffectType.GrayscaleEffect:
								{
									if (effectInterop.GetSourceCount() == 1 && effectInterop.GetSource(0) is IGraphicsEffectSource source)
									{
										SKImageFilter sourceFilter = GenerateEffectFilter(source, bounds);
										if (sourceFilter is null)
											return null;

										return SKImageFilter.CreateColorFilter(
											SKColorFilter.CreateColorMatrix(
												new float[] // Grayscale Matrix
												{
													0.21f, 0.72f, 0.07f, 0, 0,
													0.21f, 0.72f, 0.07f, 0, 0,
													0.21f, 0.72f, 0.07f, 0, 0,
													0,     0,     0,     1, 0
												}),
											sourceFilter, new(bounds));
									}

									return null;
								}
							case EffectType.InvertEffect:
								{
									if (effectInterop.GetSourceCount() == 1 && effectInterop.GetSource(0) is IGraphicsEffectSource source)
									{
										SKImageFilter sourceFilter = GenerateEffectFilter(source, bounds);
										if (sourceFilter is null)
											return null;

										return SKImageFilter.CreateColorFilter(
											SKColorFilter.CreateColorMatrix(
												new float[] // Invert Matrix
												{
													-1, 0,  0,  0, 1,
													0,  -1, 0,  0, 1,
													0,  0,  -1, 0, 1,
													0,  0,  0,  1, 0,
												}),
											sourceFilter, new(bounds));
									}

									return null;
								}
							case EffectType.HueRotationEffect:
								{
									if (effectInterop.GetSourceCount() == 1 && effectInterop.GetPropertyCount() == 1 && effectInterop.GetSource(0) is IGraphicsEffectSource source)
									{
										SKImageFilter sourceFilter = GenerateEffectFilter(source, bounds);
										if (sourceFilter is null)
											return null;

										effectInterop.GetNamedPropertyMapping("Angle", out uint angleProp, out GraphicsEffectPropertyMapping angleMapping);
										float angle = (float)effectInterop.GetProperty(angleProp);

										if (angleMapping == GraphicsEffectPropertyMapping.RadiansToDegrees)
											angle *= 180.0f / MathF.PI;

										return SKImageFilter.CreateColorFilter(
											SKColorFilter.CreateColorMatrix(
												new float[] // Hue Rotation Matrix
												{
													(0.2127f + MathF.Cos(angle) * 0.7873f - MathF.Sin(angle) * 0.2127f), (0.715f - MathF.Cos(angle) * 0.715f - MathF.Sin(angle) * 0.715f), (0.072f - MathF.Cos(angle) * 0.072f + MathF.Sin(angle) * 0.928f), 0, 0,
													(0.2127f - MathF.Cos(angle) * 0.213f + MathF.Sin(angle) * 0.143f),   (0.715f + MathF.Cos(angle) * 0.285f + MathF.Sin(angle) * 0.140f), (0.072f - MathF.Cos(angle) * 0.072f - MathF.Sin(angle) * 0.283f), 0, 0,
													(0.2127f - MathF.Cos(angle) * 0.213f - MathF.Sin(angle) * 0.787f),   (0.715f - MathF.Cos(angle) * 0.715f + MathF.Sin(angle) * 0.715f), (0.072f + MathF.Cos(angle) * 0.928f + MathF.Sin(angle) * 0.072f), 0, 0,
													0,                                                                   0,                                                                0,                                                                1, 0
												}),
											sourceFilter, new(bounds));
									}

									return null;
								}
							case EffectType.TintEffect:
								{
									if (effectInterop.GetSourceCount() == 1 && effectInterop.GetPropertyCount() >= 1 /* only the Color property is required */ && effectInterop.GetSource(0) is IGraphicsEffectSource source)
									{
										SKImageFilter sourceFilter = GenerateEffectFilter(source, bounds);
										if (sourceFilter is null)
											return null;

										// Note: ColorHdr isn't supported by Composition (as of 10.0.25941.1000)
										effectInterop.GetNamedPropertyMapping("Color", out uint colorProp, out _);
										effectInterop.GetNamedPropertyMapping("ClampOutput", out uint clampProp, out _);

										Color color = (Color)effectInterop.GetProperty(colorProp);
										bool clamp = clampProp != 0xFF ? (bool)effectInterop.GetProperty(clampProp) : false;

										string shader = $@"
											uniform shader input;
											uniform vec4 color;

											half4 main() 
											{{
												return {(clamp ? "clamp(" : String.Empty)}sample(input) * color{(clamp ? ", 0.0, 1.0)" : String.Empty)};
											}}
										";

										SKRuntimeEffect runtimeEffect = SKRuntimeEffect.Create(shader, out string errors);
										if (errors is not null)
											return null;

										SKRuntimeEffectUniforms uniforms = new(runtimeEffect)
										{
											{ "color", new float[] { color.R * (1.0f / 255.0f), color.G * (1.0f / 255.0f), color.B * (1.0f / 255.0f), color.A * (1.0f / 255.0f) } }
										};
										SKRuntimeEffectChildren children = new(runtimeEffect)
										{
											{ "input", null }
										};

										return SKImageFilter.CreateColorFilter(runtimeEffect.ToColorFilter(uniforms, children), sourceFilter, new(bounds));

										// Reference (wuceffects.dll):
										/*
											void Windows::UI::Composition::TintEffectType::GenerateCode(const Windows::UI::Composition::EffectNode *node, Windows::UI::Composition::EffectGenerator *pGenerator, const char *pszOutputPixelName)
											{
												Windows::UI::Composition::StringBuilder *pStringBuilder;
												std::string strInputPixel;
												std::string strColorProperty;

												strInputPixel = pGenerator->GetInputPixelName(node, 0);
												strColorProperty = pGenerator->DeclareShaderVariableForProperty(0); // Color
  
												pStringBuilder = pGenerator->BeginPSLine();
												pStringBuilder->Append("    ");
												pStringBuilder->Append(pszOutputPixelName);
												pStringBuilder->Append(" = ");
												pStringBuilder->Append(strInputPixel.c_str(), strInputPixel.size());
												pStringBuilder->Append(" * ");
												pStringBuilder->Append(strColorProperty.c_str(), strColorProperty.size());
												pStringBuilder->Append(";");
												pStringBuilder->Append('\n');
  
												if (*(bool*)&node->m_uprgbDefaultProperties[16]) // ClampOutput, 16 = GetPropertyMetadata(1, &metatdata) ==> metatdata.cbStructOffset
												{
													Windows::UI::Composition::StringBuilder* builder = pGenerator->BeginPSLine();
													builder->Append(pszOutputPixelName);
													builder->Append(" = saturate(");
													builder->Append(pszOutputPixelName);
													builder->Append(");");
													builder->Append('\n');
												}
											}
										*/
									}

									return null;
								}
							case EffectType.BlendEffect: // TODO: Replace this with a pixel shader to get the same output as Windows
								{
									if (effectInterop.GetSourceCount() == 2 && effectInterop.GetPropertyCount() == 1 && effectInterop.GetSource(0) is IGraphicsEffectSource bg && effectInterop.GetSource(1) is IGraphicsEffectSource fg)
									{
										SKImageFilter bgFilter = GenerateEffectFilter(bg, bounds);
										if (bgFilter is null)
											return null;

										SKImageFilter fgFilter = GenerateEffectFilter(fg, bounds);
										if (fgFilter is null)
											return null;

										effectInterop.GetNamedPropertyMapping("Mode", out uint modeProp, out _);
										D2D1BlendEffectMode mode = (D2D1BlendEffectMode)effectInterop.GetProperty(modeProp);
										SKBlendMode skMode = mode.ToSkia();

										if (skMode == (SKBlendMode)0xFF) // Unsupported mode
											return null;

										return SKImageFilter.CreateBlendMode(skMode, bgFilter, fgFilter, new(bounds));
									}

									return null;
								}
							case EffectType.CompositeEffect:
								{
									if (effectInterop.GetSourceCount() > 1 && effectInterop.GetPropertyCount() == 1)
									{
										SKImageFilter currentFilter = GenerateEffectFilter(effectInterop.GetSource(0), bounds);
										if (currentFilter is null)
											return null;

										effectInterop.GetNamedPropertyMapping("Mode", out uint modeProp, out _);
										D2D1CompositeMode mode = (D2D1CompositeMode)effectInterop.GetProperty(modeProp);
										SKBlendMode skMode = mode.ToSkia();

										if (skMode == (SKBlendMode)0xFF) // Unsupported mode
											return null;

										// We have to do this manually because SKImageFilter.CreateMerge(SKImageFilter, SKImageFilter, SKBlendMode, SKImageFilter.CropRect) is obsolete.
										for (uint idx = 1; idx < effectInterop.GetSourceCount(); idx++)
										{
											SKImageFilter nextFilter = GenerateEffectFilter(effectInterop.GetSource(idx), bounds);

											if (nextFilter is not null)
												currentFilter = SKImageFilter.CreateBlendMode(skMode, currentFilter, nextFilter, new(bounds));
										}

										return currentFilter;
									}

									return null;
								}
							case EffectType.ColorSourceEffect:
								{
									if (effectInterop.GetPropertyCount() >= 1 /* only the Color property is required */)
									{
										// Note: ColorHdr isn't supported by Composition (as of 10.0.25941.1000)
										effectInterop.GetNamedPropertyMapping("Color", out uint colorProp, out _);
										Color color = (Color)effectInterop.GetProperty(colorProp);

										return SKImageFilter.CreatePaint(new SKPaint() { Color = color.ToSKColor() }, new(bounds));
									}

									return null;
								}
							case EffectType.OpacityEffect:
								{
									if (effectInterop.GetSourceCount() == 1 && effectInterop.GetPropertyCount() == 1 && effectInterop.GetSource(0) is IGraphicsEffectSource source)
									{
										SKImageFilter sourceFilter = GenerateEffectFilter(source, bounds);
										if (sourceFilter is null)
											return null;

										effectInterop.GetNamedPropertyMapping("Opacity", out uint opacityProp, out _);
										float opacity = (float)effectInterop.GetProperty(opacityProp);


										return SKImageFilter.CreateColorFilter(
											SKColorFilter.CreateColorMatrix(
												new float[] // Opacity Matrix
												{
													1, 0, 0, 0,       0,
													0, 1, 0, 0,       0,
													0, 0, 1, 0,       0,
													0, 0, 0, opacity, 0
												}),
											sourceFilter, new(bounds));
									}

									return null;
								}
							case EffectType.ContrastEffect:
								{
									if (effectInterop.GetSourceCount() == 1 && effectInterop.GetPropertyCount() >= 1 /* only the Contrast property is required */ && effectInterop.GetSource(0) is IGraphicsEffectSource source)
									{
										SKImageFilter sourceFilter = GenerateEffectFilter(source, bounds);
										if (sourceFilter is null)
											return null;

										effectInterop.GetNamedPropertyMapping("Contrast", out uint contrastProp, out _);
										effectInterop.GetNamedPropertyMapping("ClampSource", out uint clampProp, out _);

										float contrast = (float)effectInterop.GetProperty(contrastProp);
										bool clamp = clampProp != 0xFF ? (bool)effectInterop.GetProperty(clampProp) : false;

										string shader = $@"
											uniform shader input;
											uniform half contrastValue;

											half4 Premultiply(half4 color)
											{{
												color.rgb *= color.a;
												return color;
											}}

											half4 UnPremultiply(half4 color)
											{{
												color.rgb = (color.a == 0) ? half3(0, 0, 0) : (color.rgb / color.a);
												return color;
											}}

											half4 Contrast(half4 color, half contrast)
											{{
												color = UnPremultiply(color);

												half s = 1 - (3.0 / 4.0) * contrast;
												half c2 = s - 1;
												half b2 = 4 - 3 * s;
												half a2 = 2 * c2;
												half b1 = s;
												half a1 = -a2;
    
												half3 lowResult = color.rgb * (color.rgb * a1 + b1);
												half3 highResult = color.rgb * (color.rgb * a2 + b2) + c2;
    
												half3 comparisonResult = half3(0.0);
												comparisonResult.r = (color.rgb.r < 0.5) ? 1.0 : 0.0;
												comparisonResult.g = (color.rgb.g < 0.5) ? 1.0 : 0.0;
												comparisonResult.b = (color.rgb.b < 0.5) ? 1.0 : 0.0;

												color.rgb = mix(lowResult, highResult, comparisonResult);
    
												return Premultiply(color);
											}}

											half4 main() 
											{{
												return Contrast({(clamp ? "clamp(" : String.Empty)}sample(input){(clamp ? ", 0.0, 1.0)" : String.Empty)}, contrastValue);
											}}
										";

										SKRuntimeEffect runtimeEffect = SKRuntimeEffect.Create(shader, out string errors);
										if (errors is not null)
											return null;

										SKRuntimeEffectUniforms uniforms = new(runtimeEffect)
										{
											{ "contrastValue", contrast }
										};
										SKRuntimeEffectChildren children = new(runtimeEffect)
										{
											{ "input", null }
										};

										return SKImageFilter.CreateColorFilter(runtimeEffect.ToColorFilter(uniforms, children), sourceFilter, new(bounds));

										// Reference (wuceffects.dll):
										/*
											void Windows::UI::Composition::ContrastEffectType::GenerateCode(const Windows::UI::Composition::EffectNode *node, Windows::UI::Composition::EffectGenerator *pGenerator, const char *pszOutputPixelName)
											{
												Windows::UI::Composition::StringBuilder *pStringBuilder;
												std::string strInputPixel;
												std::string strContrastProperty;

												strInputPixel = pGenerator->GetInputPixelName(node, 0);
												strContrastProperty = pGenerator->DeclareShaderVariableForProperty(0); // Contrast
  
												pGenerator->AddPSInclude("D2DContrast.hlsl");
												pStringBuilder = pGenerator->BeginPSLine();
												pStringBuilder->Append("    ");
												pStringBuilder->Append(pszOutputPixelName);
												pStringBuilder->Append(" = ");
												pStringBuilder->Append(strInputPixel.c_str(), strInputPixel.size());
												pStringBuilder->Append(";");
												pStringBuilder->Append('\n');
  
												if (*(bool*)&node->m_uprgbDefaultProperties[4]) // ClampSource, 4 = GetPropertyMetadata(1, &metatdata) ==> metatdata.cbStructOffset
												{
													Windows::UI::Composition::StringBuilder* builder = pGenerator->BeginPSLine();
													builder->Append(pszOutputPixelName);
													builder->Append(" = saturate(");
													builder->Append(pszOutputPixelName);
													builder->Append(");");
													builder->Append('\n');
												}

												Windows::UI::Composition::StringBuilder* builder = pGenerator->BeginPSLine();
												builder->Append(pszOutputPixelName);
												builder->Append(" = D2DContrast(");
												builder->Append(pszOutputPixelName);
												builder->Append(", ");
												builder->Append(strContrastProperty.c_str(), strContrastProperty.size());
												builder->Append(");");
												builder->Append('\n');
											}
										*/
									}

									return null;
								}
							case EffectType.ArithmeticCompositeEffect: // TODO: support "ClampOutput" property
								{
									if (effectInterop.GetSourceCount() == 2 && effectInterop.GetPropertyCount() >= 4 && effectInterop.GetSource(0) is IGraphicsEffectSource bg && effectInterop.GetSource(1) is IGraphicsEffectSource fg)
									{
										SKImageFilter bgFilter = GenerateEffectFilter(bg, bounds);
										if (bgFilter is null)
											return null;

										SKImageFilter fgFilter = GenerateEffectFilter(fg, bounds);
										if (fgFilter is null)
											return null;

										float? multiplyAmount = effectInterop.GetProperty(0) as float?;
										float? source1Amount = effectInterop.GetProperty(1) as float?;
										float? source2Amount = effectInterop.GetProperty(2) as float?;
										float? offset = effectInterop.GetProperty(3) as float?;

										if (multiplyAmount is null || source1Amount is null || source2Amount is null || offset is null)
										{
											float[] coefficients = effectInterop.GetProperty(0) as float[];

											if (coefficients is null)
											{
												effectInterop.GetNamedPropertyMapping("Coefficients", out uint coefficientsProp, out GraphicsEffectPropertyMapping coefficientsMapping);
												if (coefficientsMapping == GraphicsEffectPropertyMapping.Direct)
													coefficients = effectInterop.GetProperty(coefficientsProp) as float[];
												else
													return null;
											}

											if (coefficients is not null && coefficients.Length == 4)
											{
												multiplyAmount = coefficients[0];
												source1Amount = coefficients[1];
												source2Amount = coefficients[2];
												offset = coefficients[3];
											}
											else
												return null;
										}

										return SKImageFilter.CreateArithmetic(multiplyAmount.Value, source1Amount.Value, source2Amount.Value, offset.Value, false, bgFilter, fgFilter, new(bounds));
									}

									return null;
								}
							case EffectType.ExposureEffect: // TODO: We can probably replace the pixel shader with a color matrix filter instead?
								{
									if (effectInterop.GetSourceCount() == 1 && effectInterop.GetPropertyCount() == 1 && effectInterop.GetSource(0) is IGraphicsEffectSource source)
									{
										SKImageFilter sourceFilter = GenerateEffectFilter(source, bounds);
										if (sourceFilter is null)
											return null;

										effectInterop.GetNamedPropertyMapping("Exposure", out uint exposureProp, out _);

										float exposure = (float)effectInterop.GetProperty(exposureProp);
										float multiplier = MathF.Pow(2.0f, exposure);

										string shader = $@"
											uniform shader input;
											uniform half multiplier;

											half4 main() 
											{{
												half4 inputColor = sample(input);
												return half4(inputColor.rgb * multiplier, inputColor.a);
											}}
										";

										SKRuntimeEffect runtimeEffect = SKRuntimeEffect.Create(shader, out string errors);
										if (errors is not null)
											return null;

										SKRuntimeEffectUniforms uniforms = new(runtimeEffect)
										{
											{ "multiplier", multiplier }
										};
										SKRuntimeEffectChildren children = new(runtimeEffect)
										{
											{ "input", null }
										};

										return SKImageFilter.CreateColorFilter(runtimeEffect.ToColorFilter(uniforms, children), sourceFilter, new(bounds));

										// Reference (wuceffects.dll):
										/*
											void Windows::UI::Composition::ExposureEffectType::GenerateCode(const Windows::UI::Composition::EffectNode *node, Windows::UI::Composition::EffectGenerator *pGenerator, const char *pszOutputPixelName)
											{
												Windows::UI::Composition::StringBuilder *pStringBuilder;
												std::string strInputPixel;
												std::string strMultiplierProperty;
												bool isPropertyDynamic;

												strInputPixel = pGenerator->GetInputPixelName(node, 0);

												isPropertyDynamic = node->IsPropertyDynamic(0); // ExposureValue
												pGenerator->DeclareShaderVariable(
													&strMultiplierProperty,
													DCOMPOSITION_EXPRESSION_TYPE_SCALAR,
													"Multiplier",
													isPropertyDynamic,
													[](void** defaultProperties, void** output) { *(float*)*output = pow(2.0, *(float*)(defaultProperties[0])); }
												);
  
												pStringBuilder = pGenerator->BeginPSLine();
												pStringBuilder->Append(pszOutputPixelName);
												pStringBuilder->Append(" = minfloat4(");
												pStringBuilder->Append(strInputPixel.c_str(), strInputPixel.size());
												pStringBuilder->Append(".rgb * ");
												pStringBuilder->Append(strMultiplierProperty.c_str(), strMultiplierProperty.size());
												pStringBuilder->Append(", ");
												pStringBuilder->Append(strInputPixel.c_str(), strInputPixel.size());
												pStringBuilder->Append(".a);");
												pStringBuilder->Append('\n');
											}
										*/
									}

									return null;
								}
							case EffectType.CrossFadeEffect: // TODO: We should use SkColorFilters::Lerp instead once SkiaSharp includes it
								{
									if (effectInterop.GetSourceCount() == 2 && effectInterop.GetPropertyCount() == 1 && effectInterop.GetSource(0) is IGraphicsEffectSource sourceA && effectInterop.GetSource(1) is IGraphicsEffectSource sourceB)
									{
										SKImageFilter sourceFilter1 = GenerateEffectFilter(sourceB, bounds);
										if (sourceFilter1 is null)
											return null;

										SKImageFilter sourceFilter2 = GenerateEffectFilter(sourceA, bounds);
										if (sourceFilter2 is null)
											return null;

										effectInterop.GetNamedPropertyMapping("CrossFade", out uint crossfadeProp, out _);

										float crossfade = (float)effectInterop.GetProperty(crossfadeProp);

										if (crossfade <= 0.0f)
											return sourceFilter1;
										else if (crossfade >= 1.0f)
											return sourceFilter2;

										SKImageFilter fbFilter = SKImageFilter.CreateColorFilter(
											SKColorFilter.CreateColorMatrix(
												new float[]
												{
													crossfade, 0,         0,         0,         0,
													0,         crossfade, 0,         0,         0,
													0,         0,         crossfade, 0,         0,
													0,         0,         0,         crossfade, 0
												}),
											sourceFilter2);

										string shader = $@"
											uniform shader input;
											uniform half crossfade;

											half4 main() 
											{{
												half4 inputColor = sample(input);
												return inputColor - (inputColor * crossfade);
											}}
										";

										SKRuntimeEffect runtimeEffect = SKRuntimeEffect.Create(shader, out string errors);
										if (errors is not null)
											return null;

										SKRuntimeEffectUniforms uniforms = new(runtimeEffect)
										{
											{ "crossfade", crossfade }
										};
										SKRuntimeEffectChildren children = new(runtimeEffect)
										{
											{ "input", null }
										};

										SKImageFilter amafFilter = SKImageFilter.CreateColorFilter(runtimeEffect.ToColorFilter(uniforms, children), sourceFilter1);

										return SKImageFilter.CreateBlendMode(SKBlendMode.Plus, fbFilter, amafFilter, new(bounds));
									}

									return null;
								}
							case EffectType.LuminanceToAlphaEffect:
								{
									if (effectInterop.GetSourceCount() == 1 && effectInterop.GetSource(0) is IGraphicsEffectSource source)
									{
										SKImageFilter sourceFilter = GenerateEffectFilter(source, bounds);
										if (sourceFilter is null)
											return null;

										return SKImageFilter.CreateColorFilter(SKColorFilter.CreateLumaColor(), sourceFilter, new(bounds));
									}

									return null;
								}
							case EffectType.LinearTransferEffect:
								{
									if (effectInterop.GetSourceCount() == 1 && effectInterop.GetPropertyCount() == 13 && effectInterop.GetSource(0) is IGraphicsEffectSource source)
									{
										SKImageFilter sourceFilter = GenerateEffectFilter(source, bounds);
										if (sourceFilter is null)
											return null;

										effectInterop.GetNamedPropertyMapping("RedOffset", out uint redOffsetProp, out _);
										effectInterop.GetNamedPropertyMapping("RedSlope", out uint redSlopeProp, out _);
										effectInterop.GetNamedPropertyMapping("RedDisable", out uint redDisableProp, out _);

										effectInterop.GetNamedPropertyMapping("GreenOffset", out uint greenOffsetProp, out _);
										effectInterop.GetNamedPropertyMapping("GreenSlope", out uint greenSlopeProp, out _);
										effectInterop.GetNamedPropertyMapping("GreenDisable", out uint greenDisableProp, out _);

										effectInterop.GetNamedPropertyMapping("BlueOffset", out uint blueOffsetProp, out _);
										effectInterop.GetNamedPropertyMapping("BlueSlope", out uint blueSlopeProp, out _);
										effectInterop.GetNamedPropertyMapping("BlueDisable", out uint blueDisableProp, out _);

										effectInterop.GetNamedPropertyMapping("AlphaOffset", out uint alphaOffsetProp, out _);
										effectInterop.GetNamedPropertyMapping("AlphaSlope", out uint alphaSlopeProp, out _);
										effectInterop.GetNamedPropertyMapping("AlphaDisable", out uint alphaDisableProp, out _);

										effectInterop.GetNamedPropertyMapping("ClampOutput", out uint clampProp, out _);

										float redOffset = (float)effectInterop.GetProperty(redOffsetProp);
										float redSlope = (float)effectInterop.GetProperty(redSlopeProp);
										bool redDisable = (bool)effectInterop.GetProperty(redDisableProp);

										float greenOffset = (float)effectInterop.GetProperty(greenOffsetProp);
										float greenSlope = (float)effectInterop.GetProperty(greenSlopeProp);
										bool greenDisable = (bool)effectInterop.GetProperty(greenDisableProp);

										float blueOffset = (float)effectInterop.GetProperty(blueOffsetProp);
										float blueSlope = (float)effectInterop.GetProperty(blueSlopeProp);
										bool blueDisable = (bool)effectInterop.GetProperty(blueDisableProp);

										float alphaOffset = (float)effectInterop.GetProperty(alphaOffsetProp);
										float alphaSlope = (float)effectInterop.GetProperty(alphaSlopeProp);
										bool alphaDisable = (bool)effectInterop.GetProperty(alphaDisableProp);

										bool clamp = clampProp != 0xFF ? (bool)effectInterop.GetProperty(clampProp) : false;

										string shader = $@"
											uniform shader input;

											uniform half redOffset;
											uniform half redSlope;

											uniform half greenOffset;
											uniform half greenSlope;

											uniform half blueOffset;
											uniform half blueSlope;

											uniform half alphaOffset;
											uniform half alphaSlope;

											half4 Premultiply(half4 color)
											{{
												color.rgb *= color.a;
												return color;
											}}

											half4 UnPremultiply(half4 color)
											{{
												color.rgb = (color.a == 0) ? half3(0, 0, 0) : (color.rgb / color.a);
												return color;
											}}

											half4 main()
											{{
												half4 color = UnPremultiply(sample(input));
												color = half4(
													{(redDisable ? "color.r" : "redOffset + color.r * redSlope")},
													{(greenDisable ? "color.g" : "greenOffset + color.g * greenSlope")},
													{(blueDisable ? "color.b" : "blueOffset + color.b * blueSlope")},
													{(alphaDisable ? "color.a" : "alphaOffset + color.a * alphaSlope")}
												);

												return {(clamp ? "clamp(" : String.Empty)}Premultiply(color){(clamp ? ", 0.0, 1.0)" : String.Empty)};
											}}
										";

										SKRuntimeEffect runtimeEffect = SKRuntimeEffect.Create(shader, out string errors);
										if (errors is not null)
											return null;

										SKRuntimeEffectUniforms uniforms = new(runtimeEffect)
										{
											{ "redOffset", redOffset },
											{ "redSlope", redSlope },

											{ "greenOffset", greenOffset },
											{ "greenSlope", greenSlope },

											{ "blueOffset", blueOffset },
											{ "blueSlope", blueSlope },

											{ "alphaOffset", alphaOffset },
											{ "alphaSlope", alphaSlope }
										};
										SKRuntimeEffectChildren children = new(runtimeEffect)
										{
											{ "input", null }
										};

										return SKImageFilter.CreateColorFilter(runtimeEffect.ToColorFilter(uniforms, children), sourceFilter, new(bounds));

										// Reference (wuceffects.dll):
										/*
											void Windows::UI::Composition::LinearTransferEffectType::GenerateCode(const Windows::UI::Composition::EffectNode *node, Windows::UI::Composition::EffectGenerator *pGenerator, const char *pszOutputPixelName)
											{
												bool rgfDisable[4];
												std::string strInputPixel;
												std::string rgstrPropertyNames[8];
												Windows::UI::Composition::StringBuilder *pStringBuilder;
	
												strInputPixel = pGenerator->GetInputPixelName(node, 0);
	
												for ( int i = 0; i < 13; i += 3 )
												{
													rgstrPropertyNames[i] = pGenerator->DeclareShaderVariableForProperty(i); // Offset
													rgstrPropertyNames[i + 1] = pGenerator->DeclareShaderVariableForProperty(i + 1); // Slope
												}
	
												pStringBuilder = pGenerator->BeginPSLine();
												pStringBuilder->Append(pszOutputPixelName);
												pStringBuilder->Append(" = UnPremultiply(");
												pStringBuilder->Append(strInputPixel.c_str(), strInputPixel.size());
												pStringBuilder->Append(");");
												pStringBuilder->Append('\n');
	
												pStringBuilder->Append(pszOutputPixelName);
												pStringBuilder->Append(" = minfloat4(");
												pStringBuilder->Append('\n');
	
												rgfDisable[0] = *(bool*)&node->m_uprgbDefaultProperties[32]; // RedDisable
												rgfDisable[1] = *(bool*)&node->m_uprgbDefaultProperties[33]; // GreenDisable
												rgfDisable[2] = *(bool*)&node->m_uprgbDefaultProperties[34]; // BlueDisable
												rgfDisable[3] = *(bool*)&node->m_uprgbDefaultProperties[35]; // AlphaDisable
	
												const char* RGBA = "rgba";
												for ( int i,k = 0; i < 4; i++, k += 2 )
												{
													if (i)
													{
														pStringBuilder->Append(",");
														pStringBuilder->Append('\n');
													}
		
													if ( rgfDisable[i] )
													{
														pStringBuilder->Append(pszOutputPixelName);
														pStringBuilder->Append('.');
														pStringBuilder->Append(RGBA[i]);
													}
													else
													{
														pStringBuilder->Append(rgstrPropertyNames[k]); // Offset
														pStringBuilder->Append(" + ");
														pStringBuilder->Append(pszOutputPixelName);
														pStringBuilder->Append('.');
														pStringBuilder->Append(RGBA[i]);
														pStringBuilder->Append(" * ");
														pStringBuilder->Append(rgstrPropertyNames[k + 1]); // Slope
													}
												}
	
												pStringBuilder->Append(");");
												pStringBuilder->Append('\n');
	
												pStringBuilder->Append(pszOutputPixelName);
												pStringBuilder->Append(" = Premultiply(");
												pStringBuilder->Append(pszOutputPixelName);
												pStringBuilder->Append(");");
												pStringBuilder->Append('\n');
	
												if (*(bool*)&node->m_uprgbDefaultProperties[36]) // ClampOutput
												{
													pStringBuilder->Append(pszOutputPixelName);
													pStringBuilder->Append(" = saturate(");
													pStringBuilder->Append(pszOutputPixelName);
													pStringBuilder->Append(");");
													pStringBuilder->Append('\n');
												}
											}
										*/
									}

									return null;
								}
							case EffectType.Unsupported:
							default:
								return null;
						}
					}
				default:
					return null;
			}
		}

		internal override void UpdatePaint(SKPaint paint, SKRect bounds)
		{
			SKImageFilter filter = GenerateEffectFilter(_effect, bounds);
			if (filter is null)
				throw new NotSupportedException("The specified IGraphicsEffect is not supported"); // TODO: Replicate Windows' behavior

			paint.Shader = null;
			paint.ImageFilter = filter;
			paint.FilterQuality = SKFilterQuality.High;
		}
	}
}
