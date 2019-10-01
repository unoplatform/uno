using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Uno.UI.Views.Controls;
using Uno.Extensions;
using System;
using System.Collections.Generic;
using Uno.Disposables;
using System.Text;
using Uno.UI.Converters;
using Uno.UI.Controls;
using Uno;
using Windows.UI.Xaml.Media;

#if XAMARIN_IOS_UNIFIED
using UIKit;
#elif XAMARIN_IOS
using MonoTouch.UIKit;
#endif

namespace Windows.UI.Xaml
{
	public static partial class GenericStyles
	{
		static partial void InitStyles()
		{
			InitItemsControl();
			InitFlipView();
			InitWebView();
			InitListViewItem();
			InitGridViewItem();
			InitFlipViewItem();
			InitFlyoutPresenter();
			InitDatePicker();
			InitDatePickerSelector();

#if !IS_UNO
			InitExpander();
			InitShowControl();
			InitIfDataContextControl();
			InitAVP();
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

		private static void InitDatePicker()
        {
            var style = new Style(typeof(Windows.UI.Xaml.Controls.DatePicker))
            {
                Setters =  {
                    new Setter<DatePicker>("Template", t =>
                        t.Template = new ControlTemplate(()=>new UIDatePicker())                        
                    )
                }
            };

			Style.RegisterDefaultStyleForType(typeof(Windows.UI.Xaml.Controls.DatePicker), style);
		}

		private static void InitDatePickerSelector()
		{
			var style = new Style(typeof(Windows.UI.Xaml.Controls.DatePickerSelector))
			{
				Setters =  {
					new Setter<DatePickerSelector>("Template", t =>
						t.Template = new ControlTemplate(() =>
						{
							return new Border
							{
								Background = SolidColorBrushHelper.White,
								Margin = new Thickness(5),
								CornerRadius = 10,
								Child = new UIDatePicker()
							};
						})
					)
				}
			};

			Style.RegisterDefaultStyleForType(typeof(Windows.UI.Xaml.Controls.DatePickerSelector), style);
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

		private static void InitListViewItem()
		{
			var listViewItemStyle = new Style(typeof(ListViewItem))
			{
				Setters =  {
					new Setter<ListViewItem>("Template", t =>
						t.Template = Funcs.Create(() =>
							new Grid()
							{
								Children = {
									new ContentPresenter {
									}
									.Binding("Content", new TemplateBinding("Content"))
									.Binding("ContentTemplate", new TemplateBinding("ContentTemplate"))
									.Binding("ContentTemplateSelector", new TemplateBinding("ContentTemplateSelector")),
									new Border {
										BorderBrush = SolidColorBrushHelper.Blue,
										BorderThickness = new Thickness(2),
									}
									.Binding("Visibility", new TemplateBinding("IsSelected", converter: new UnoNativeDefaultProgressBarReverseBoolConverter())),
								}
							}
						)
					),
				}
			};

			Style.RegisterDefaultStyleForType(typeof(ListViewItem), listViewItemStyle);
		}
		private static void InitGridViewItem()
		{
			var gridViewItemStyle = new Style(typeof(GridViewItem))
			{
				Setters =  {
					new Setter<GridViewItem>("Template", t =>
						t.Template = Funcs.Create(() =>
							new Grid()
							{
								Children = {
									new ContentPresenter {
									}
									.Binding("Content", new TemplateBinding("Content"))
									.Binding("ContentTemplate", new TemplateBinding("ContentTemplate"))
									.Binding("ContentTemplateSelector", new TemplateBinding("ContentTemplateSelector")),
									new Border {
										BorderBrush = SolidColorBrushHelper.Blue,
										BorderThickness = new Thickness(2),
									}
									.Binding("Visibility", new TemplateBinding("IsSelected", converter: new UnoNativeDefaultProgressBarReverseBoolConverter())),
								}
							}
						)
					),
				}
			};

			Style.RegisterDefaultStyleForType(typeof(GridViewItem), gridViewItemStyle);
		}
		private static void InitFlipViewItem()
		{
			var flipViewItemStyle = new Style(typeof(FlipViewItem))
			{
				Setters =  {
					new Setter<FlipViewItem>("Template", t =>
						t.Template = Funcs.Create(() =>
							new Grid()
							{
								Children = {
									new ContentPresenter {
									}
									.Binding("Content", new TemplateBinding("Content"))
									.Binding("ContentTemplate", new TemplateBinding("ContentTemplate"))
									.Binding("ContentTemplateSelector", new TemplateBinding("ContentTemplateSelector")),
								}
							}
						)
					),
				}
			};

			Style.RegisterDefaultStyleForType(typeof(FlipViewItem), flipViewItemStyle);
		}

		private static void InitWebView()
		{
			var style = new Style(typeof(Windows.UI.Xaml.Controls.WebView))
			{
				Setters =  {
					new Setter<WebView>("Template", t =>
						t.Template = Funcs.Create<UIView>(() => (UIView)new UnoWKWebView() 
						)
					)
				}
			};

			Style.RegisterDefaultStyleForType(typeof(Windows.UI.Xaml.Controls.WebView), style);
		}

		private static void InitItemsControl()
		{
			var style = new Style(typeof(Windows.UI.Xaml.Controls.ItemsControl))
			{
				Setters =  {
					new Setter<ItemsControl>("Template", t =>
						t.Template = Funcs.Create(() =>
							new ItemsPresenter()
								.Binding(ItemsPresenter.PaddingProperty.Name, new TemplateBinding(ItemsControl.PaddingProperty.Name))
						)
					)
				}
			};

			Style.RegisterDefaultStyleForType(typeof(Windows.UI.Xaml.Controls.ItemsControl), style);
		}

		private static void InitFlipView()
		{
			var style = new Style(typeof(Windows.UI.Xaml.Controls.FlipView))
			{
				Setters =  {

					// The order is important for this template, see FlipView.UpdateItems for the
					// PagedCollectionView type dependency.
					new Setter<FlipView>("ItemsPanel", t =>
						t.ItemsPanel = new ItemsPanelTemplate(() =>
							new PagedCollectionView() {ShowsHorizontalScrollIndicator=false })
					),
					new Setter<FlipView>("Template", t =>
						t.Template = new ControlTemplate(() =>
							new ItemsPresenter()
						)
					)
				}
			};

			Style.RegisterDefaultStyleForType(typeof(Windows.UI.Xaml.Controls.FlipView), style);
		}

#if !IS_UNO
		private static void InitExpander()
		{
			var style = new Style(typeof(Expander))
			{
				Setters =  {
					new Setter<Expander>("Template", t =>
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
									}
								};
							}
						)
					)
				}
			};

			Style.RegisterDefaultStyleForType(typeof(Expander), style);
		}
#endif

		private static void InitContentControl()
		{
			var style = new Style(typeof(ContentControl))
			{
				Setters =  {
					new Setter<ContentControl>("Template", t =>
						t.Template = Funcs.Create(() =>
							new ContentPresenter()
								.Binding("Content", new TemplateBinding("Content"))
								.Binding("ContentTemplate", new TemplateBinding("ContentTemplate"))
								.Binding("Margin", new TemplateBinding("Padding"))
								.Binding("HorizontalAlignment", new TemplateBinding("HorizontalContentAlignment"))
								.Binding("VerticalAlignment", new TemplateBinding("VerticalContentAlignment"))
						)
					),
					new Setter<ContentControl>("HorizontalContentAlignment", t => t.HorizontalContentAlignment = HorizontalAlignment.Left),
					new Setter<ContentControl>("VerticalContentAlignment", t => t.VerticalContentAlignment = VerticalAlignment.Top)
				}
			};

			Style.RegisterDefaultStyleForType(typeof(ContentControl), style);
		}

		private static void InitFlyoutPresenter()
		{
			var style = new Style(typeof(FlyoutPresenter))
			{
				Setters =  {
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

			Style.RegisterDefaultStyleForType(typeof(FlyoutPresenter), style);
		}
	}
}
