using Uno.UI.Controls;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using Uno.Disposables;
using System.Text;
using Uno.UI.Converters;
using Android.Util;
using Uno.UI;
using Uno;

#if XAMARIN_ANDROID
using View = Android.Views.View;
using ViewGroup = Android.Views.ViewGroup;
using Font = Android.Graphics.Typeface;
using Android.Graphics;
#elif XAMARIN_IOS
using View = MonoTouch.UIKit.UIView;
using ViewGroup = MonoTouch.UIKit.UIView;
using Color = MonoTouch.UIKit.UIColor;
using Font = MonoTouch.UIKit.UIFont;
#endif

namespace Windows.UI.Xaml
{
	public static partial class GenericStyles
	{
		static partial void InitStyles()
		{
			InitFlyoutPresenter();

#if !IS_UNO
			InitExpander();
			InitShowControl();
			InitAsyncValuePresenter();
			InitIfDataContextControl();
			InitStructuredContentPresenter();
#endif

			// WARNING: For compatibility reasons, the default template of a ContentControl is NULL
			//			which makes the ContentControl skip the ContentPresenter level.
			//			This is critical for android where the ViewGroup nesting is severely limited 
			//			in Android 4.4 and lower, where the UI thread stack size is 32KB or below.
			//
			//          This line is intentionally left here to remind of the reason why
			//          there is no ContentControl template, please do not remove.
			// InitContentControl();
		}

#if !IS_UNO
		private static void InitStructuredContentPresenter()
		{
			var style = new Style(typeof(StructuredContentPresenter))
			{
				Setters =
				{
					new Setter<StructuredContentPresenter>("Template", pb => pb
						.Template = new ControlTemplate(() => new ContentControl {
							Name = "PART_Root",
							HorizontalContentAlignment = HorizontalAlignment.Stretch,
							VerticalContentAlignment = VerticalAlignment.Stretch,
						})
					)
				}
			};

			Style.RegisterDefaultStyleForType(typeof(StructuredContentPresenter), style);
		}
#endif

#if !IS_UNO
		private static void InitExpander()
		{
			var style = new Style(typeof(Uno.UI.Controls.Expander))
			{
				Setters = {
					new Setter<Uno.UI.Controls.Expander>("Template", t =>
						t.Template = new ControlTemplate(() =>
							{
								return new StackPanel {
									Children = {
										new ToggleButton {
										}
										.Binding("IsChecked", new TemplateBinding("IsOpened"))
										.Binding("Content", new TemplateBinding("HeaderContent"))
										.Binding("ContentTemplate", new TemplateBinding("HeaderTemplate")),

										new ContentPresenter {
										}
										.Binding("Content", new TemplateBinding("Content"))
										.Binding("ContentTemplate", new TemplateBinding("ContentTemplate"))
									},
								};
							}
						)
					)
				}
			};

			Style.RegisterDefaultStyleForType(typeof(Uno.UI.Controls.Expander), style);
		}
#endif

		private static void InitFlyoutPresenter()
		{
			var style = new Style(typeof(FlyoutPresenter))
			{
				Setters = {
					new Setter<FlyoutPresenter>("Template", t =>
						t.Template = Funcs.Create(() =>
							new Border()
							{
								Child = new ContentPresenter()
									.Binding("Content", new TemplateBinding("Content"))
									.Binding("ContentTemplate", new TemplateBinding("ContentTemplate"))
									.Binding("Margin", new TemplateBinding("Padding"))
									.Binding("HorizontalAlignment", new TemplateBinding("HorizontalContentAlignment"))
									.Binding("VerticalAlignment", new TemplateBinding("VerticalContentAlignment"))
							}
							.Binding("Background", new TemplateBinding("Background"))
							.Binding("BorderBrush", new TemplateBinding("BorderBrush"))
							.Binding("BorderThickness", new TemplateBinding("BorderThickness"))
						)
					),
					new Setter<FlyoutPresenter>("HorizontalContentAlignment", t => t.HorizontalContentAlignment = HorizontalAlignment.Stretch),
					new Setter<FlyoutPresenter>("VerticalContentAlignment", t => t.VerticalContentAlignment = VerticalAlignment.Stretch)
				}
			};

			Style.RegisterDefaultStyleForType(typeof(FlyoutPresenter), () => style);
		}
	}
}
