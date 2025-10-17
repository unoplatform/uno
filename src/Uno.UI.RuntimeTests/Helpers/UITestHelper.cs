#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Input.Preview.Injection;
using Windows.UI.Popups;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Markup;
using Private.Infrastructure;
using SamplesApp.UITests;

#if !HAS_UNO
using System.Runtime.InteropServices;
#endif

#if HAS_UNO_WINUI || WINAPPSDK
using PointerDeviceType = Microsoft.UI.Input.PointerDeviceType;
#else
using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;
#endif

namespace Uno.UI.RuntimeTests.Helpers;

// Note: This file contains a bunch of helpers that are expected to be moved to the test engine among the pointer injection work

public static class UITestHelper
{
	/// <summary>
	/// Loads an element onto the test area, and wait for it to be loaded before returning.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="element">The element to be loaded onto the test area.</param>
	/// <param name="isLoaded">optional, function to override the default is-loaded check.</param>
	/// <returns>Loaded element absolute bounds.</returns>
	/// <remarks>The default is-loaded check fails on 0 height/width, or empty list-view, overload the <paramref name="isLoaded"/> with <code>x => x.IsLoaded</code> or <code>x => x.GetTemplateRoot() != null</code> to bypass that.</remarks>
	public static async Task<Rect> Load<T>(T element, Func<T, bool>? isLoaded = null) where T : FrameworkElement
	{
		TestServices.WindowHelper.WindowContent = element;

		await WaitForLoaded(element, isLoaded);

		return element.GetAbsoluteBounds();
	}

	public static async Task WaitForLoaded<T>(T element, Func<T, bool>? isLoaded = null) where T : FrameworkElement
	{
		if (isLoaded is null)
		{
			await TestServices.WindowHelper.WaitForLoaded(element);
		}
		else
		{
			await TestServices.WindowHelper.WaitFor(() => isLoaded(element), message: $"Timeout waiting on {element} to be loaded with custom criteria.");
		}
		await TestServices.WindowHelper.WaitForIdle();
	}

	public static Task WaitFor(
		Func<bool> condition,
		int timeoutMS = 1000,
		string? message = null,
		[CallerMemberName] string? callerMemberName = null,
		[CallerLineNumber] int lineNumber = 0
	) => TestServices.WindowHelper.WaitFor(condition, timeoutMS, message, callerMemberName, lineNumber);

	public static async Task WaitForRender(
		int frameCount = 1,
		int timeoutMS = 1000,
		string? message = null,
		[CallerMemberName] string? callerMemberName = null,
		[CallerLineNumber] int lineNumber = 0)
	{
#if __SKIA__
		var renderingCount = 0;
		EventHandler<object> callback = (_, _) => renderingCount++;
		CompositionTarget.Rendering += callback;
		try
		{
			var currentRenderingCount = renderingCount;
			await WaitFor(() => renderingCount - currentRenderingCount >= frameCount, timeoutMS, message, callerMemberName, lineNumber);
		}
		finally
		{
			CompositionTarget.Rendering -= callback;
		}
#else
		await WaitForIdle();
		await Task.Delay(timeoutMS);
#endif
	}


	public static async Task WaitForIdle(bool waitForCompositionAnimations = false)
	{
#if __SKIA__
		do
		{
			await TestServices.WindowHelper.WaitForIdle();
		} while (waitForCompositionAnimations && (TestServices.WindowHelper.WindowContent?.Visual?.Compositor.IsAnimating ?? false));
#else
		await TestServices.WindowHelper.WaitForIdle();
#endif
	}

	/// <summary>
	/// Takes a screen-shot of the given element.
	/// </summary>
	/// <param name="element">The element to screen-shot.</param>
	/// <param name="opaque">Indicates if the resulting image should be make opaque (i.e. all pixels has an opacity of 0xFF) or not.</param>
	/// <param name="scaling">Indicates the scaling strategy to apply for the image (when screen is not using a 1.0 scale, usually 4K screens).</param>
	/// <returns></returns>
	public static async Task<RawBitmap> ScreenShot(FrameworkElement element, bool opaque = false, ScreenShotScalingMode scaling = ScreenShotScalingMode.UsePhysicalPixelsWithImplicitScaling)
	{
#if HAS_RENDER_TARGET_BITMAP
		var renderer = new RenderTargetBitmap();
		element.UpdateLayout();
		await TestServices.WindowHelper.WaitForIdle();

		RawBitmap bitmap;
		switch (scaling)
		{
			case ScreenShotScalingMode.UsePhysicalPixelsWithImplicitScaling:
				await renderer.RenderAsync(element);
				bitmap = await RawBitmap.From(renderer, element, element.XamlRoot?.RasterizationScale ?? 1);
				break;
			case ScreenShotScalingMode.UseLogicalPixels:
				await renderer.RenderAsync(element, (int)element.RenderSize.Width, (int)element.RenderSize.Height);
				bitmap = await RawBitmap.From(renderer, element);
				break;
			case ScreenShotScalingMode.UsePhysicalPixels:
				await renderer.RenderAsync(element);
				bitmap = await RawBitmap.From(renderer, element);
				break;
			default:
				throw new NotSupportedException($"Mode {scaling} is not supported.");
		}

		if (opaque)
		{
			bitmap.MakeOpaque();
		}

		return bitmap;
#else
		throw new NotSupportedException("Cannot take screenshot on this platform.");
#endif
	}

	public enum ScreenShotScalingMode
	{
		/// <summary>
		/// Screen-shot is made at full resolution, then the returned RawBitmap is configured to implicitly apply screen scaling
		/// to requested pixel coordinates in <see cref="RawBitmap.GetPixel"/> method.
		///
		/// This is best / common option has it avoids artifacts due image scaling while still allowing to use logical pixels.
		/// </summary>
		UsePhysicalPixelsWithImplicitScaling,

		/// <summary>
		/// Screen-shot is made at full resolution, and access to the returned <see cref="RawBitmap"/> are assumed to be in physical pixels.
		/// </summary>
		UsePhysicalPixels,

		/// <summary>
		/// Screen-shot is forcefully scaled down to logical pixels.
		/// </summary>
		UseLogicalPixels
	}

	/// <summary>
	/// Shows the given screenshot on screen for debug purposes
	/// </summary>
	/// <param name="bitmap">The image to show.</param>
	/// <returns></returns>
	public static async Task Show(RawBitmap bitmap)
	{
		Image img;
		CompositeTransform imgTr;
		TextBlock pos;
		var popup = new ContentDialog
		{
			XamlRoot = TestServices.WindowHelper.XamlRoot,
			MinWidth = bitmap.Width + 2,
			MinHeight = bitmap.Height + 30,
			Content = new Grid
			{
				RowDefinitions =
				{
					new RowDefinition(),
					new RowDefinition { Height = GridLength.Auto }
				},
				Children =
				{
					new Border
					{
						BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Black),
						BorderThickness = new Thickness(1),
						Background = new SolidColorBrush(Microsoft.UI.Colors.Gray),
						Width = bitmap.Width * bitmap.ImplicitScaling + 2,
						Height = bitmap.Height * bitmap.ImplicitScaling + 2,
						Child = img = new Image
						{
							Width = bitmap.Width * bitmap.ImplicitScaling,
							Height = bitmap.Height * bitmap.ImplicitScaling,
							Source = await bitmap.GetImageSource(),
							Stretch = Stretch.None,
							ManipulationMode = ManipulationModes.Scale
								| ManipulationModes.ScaleInertia
								| ManipulationModes.TranslateX
								| ManipulationModes.TranslateY
								| ManipulationModes.TranslateInertia,
							RenderTransformOrigin = new Point(.5, .5),
							RenderTransform = imgTr = new CompositeTransform()
						}
					},
					new StackPanel
					{
						Orientation = Orientation.Horizontal,
						HorizontalAlignment = HorizontalAlignment.Right,
						Children =
						{
							(pos = new TextBlock
							{
								Text = $"{bitmap.Width}x{bitmap.Height}",
								FontSize = 8
							})
						}
					}.Apply(e => Grid.SetRow(e, 1))
				}
			},
			PrimaryButtonText = "OK"
		};

		img.PointerMoved += (snd, e) => DumpState(e.GetCurrentPoint(img).Position);
		img.PointerWheelChanged += (snd, e) =>
		{
			if (e.KeyModifiers is VirtualKeyModifiers.Control
				&& e.GetCurrentPoint(img) is { Properties.IsHorizontalMouseWheel: false } point)
			{
				var factor = Math.Sign(point.Properties.MouseWheelDelta) is 1 ? 1.2 : 1 / 1.2;
				imgTr.ScaleX *= factor;
				imgTr.ScaleY *= factor;

				DumpState(point.Position);
			}
		};
		img.ManipulationDelta += (snd, e) =>
		{
			imgTr.TranslateX += e.Delta.Translation.X;
			imgTr.TranslateY += e.Delta.Translation.Y;
			imgTr.ScaleX *= e.Delta.Scale;
			imgTr.ScaleY *= e.Delta.Scale;

			DumpState(e.Position);
		};

		void DumpState(Point phyLoc)
		{
			var scaling = bitmap.ImplicitScaling;
			var virLoc = new Point(phyLoc.X / scaling, phyLoc.Y / scaling);
			var virSize = bitmap.Size;
			var phySize = new Size(virSize.Width * scaling, virSize.Height * scaling);

			if (virLoc.X >= 0 && virLoc.X < virSize.Width
				&& virLoc.Y >= 0 && virLoc.Y < virSize.Height)
			{
				if (scaling is not 1.0)
				{
					pos.Text = $"{imgTr.ScaleX:P0} {bitmap.GetPixel((int)virLoc.X, (int)virLoc.Y)} | vir: {virLoc.X:F0},{virLoc.Y:F0} / {virSize.Width}x{virSize.Height} | phy: {phyLoc.X:F0},{phyLoc.Y:F0} / {phySize.Width}x{phySize.Height}";
				}
				else
				{
					pos.Text = $"{imgTr.ScaleX:P0} {bitmap.GetPixel((int)virLoc.X, (int)virLoc.Y)} | {phyLoc.X:F0},{phyLoc.Y:F0} / {virSize.Width}x{virSize.Height}";
				}
			}
			else
			{
				pos.Text = $"{imgTr.ScaleX:P0} {bitmap.Width}x{bitmap.Height}";
			}
		}

		await popup.ShowAsync(ContentDialogPlacement.Popup);
	}

	public static void CloseAllPopups()
#if HAS_UNO
		=> VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
#else
	{
		foreach (var popup in VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.XamlRoot))
		{
			popup.IsOpen = false;
		}
	}
#endif
}

public class DynamicDataTemplate : IDisposable
{
	private static readonly Dictionary<Guid, Func<FrameworkElement>> _templates = new();

	public static FrameworkElement GetElement(string id)
		=> Guid.TryParse(id, out var guid) && _templates.TryGetValue(guid, out var factory)
			? factory()
			: new TextBlock { Text = $"Template '{id}' not found" };

	public DynamicDataTemplate(Func<FrameworkElement> factory)
	{
		_templates.Add(Id, factory);

#if HAS_UNO
		Value = new DataTemplate(factory);
#else
		Value = (DataTemplate)XamlReader.Load($@"
			<DataTemplate 
				xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
				xmlns:local=""using:{typeof(DynamicDataTemplatePresenter).Namespace}"">
				<local:DynamicDataTemplatePresenter TemplateId=""{Id}"" />
			</DataTemplate>");
#endif
	}

	public Guid Id { get; } = Guid.NewGuid();

	public DataTemplate Value { get; }

	public void Dispose()
		=> _templates.Remove(Id);

	~DynamicDataTemplate()
		=> Dispose();
}

public partial class DynamicDataTemplatePresenter : ContentPresenter
{
	public DynamicDataTemplatePresenter()
	{
		HorizontalAlignment = HorizontalAlignment.Stretch;
		VerticalAlignment = VerticalAlignment.Stretch;
		HorizontalContentAlignment = HorizontalAlignment.Stretch;
		VerticalContentAlignment = VerticalAlignment.Stretch;
	}

	public static readonly DependencyProperty TemplateIdProperty = DependencyProperty.Register(
		nameof(TemplateId),
		typeof(string),
		typeof(DynamicDataTemplatePresenter),
		new PropertyMetadata(default(string), OnTemplateIdChanged));

	private static void OnTemplateIdChanged(DependencyObject snd, DependencyPropertyChangedEventArgs args)
	{
		if (snd is DynamicDataTemplatePresenter that)
		{
			that.Content = args.NewValue is string id
				? DynamicDataTemplate.GetElement(id)
				: null;
		}
	}

	public string TemplateId
	{
		get => (string)this.GetValue(TemplateIdProperty);
		set => this.SetValue(TemplateIdProperty, value);
	}
}

public static class FrameworkElementExtensions
{
	public static Rect GetAbsoluteBounds(this FrameworkElement element)
		=> element.TransformToVisual(null).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
}
