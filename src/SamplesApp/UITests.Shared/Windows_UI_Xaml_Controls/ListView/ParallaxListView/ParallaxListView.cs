using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using Uno.UI.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SamplesApp.Windows_UI_Xaml_Controls.ListView
{
	[TemplatePart(Name = "PART_HeaderBackground", Type = typeof(Microsoft.UI.Xaml.Controls.ScrollViewer))]
	[TemplatePart(Name = "PART_HeaderForeground", Type = typeof(Microsoft.UI.Xaml.Controls.ScrollViewer))]
	[TemplatePart(Name = "PART_ListView", Type = typeof(Microsoft.UI.Xaml.Controls.ListView))]
	[TemplatePart(Name = "PART_CommandBar", Type = typeof(Microsoft.UI.Xaml.Controls.CommandBar))]
	public partial class ParallaxListView : Control
	{
		private const int CommandBarHeight = 48;
		private const int TitleOffset = 111;

		private const string HeaderBackgroundName = "PART_HeaderBackground";
		private const string HeaderForegroundName = "PART_HeaderForeground";
		private const string ScrollViewerName = "PART_ListView";
		private const string CommandBarName = "PART_CommandBar";

		private Microsoft.UI.Xaml.Controls.ScrollViewer _headerBackground;
		private Microsoft.UI.Xaml.Controls.ScrollViewer _headerForeground;
		private Microsoft.UI.Xaml.Controls.ListView _mainListview;
		private Microsoft.UI.Xaml.Controls.ScrollViewer _mainScrollViewer;
		private Microsoft.UI.Xaml.Controls.CommandBar _commandBar;

		private double _screenHeight;

		#region ItemsSource Property

		public object ItemsSource
		{
			get { return (object)GetValue(ItemsSourceProperty); }
			set { SetValue(ItemsSourceProperty, value); }
		}

		public static DependencyProperty ItemsSourceProperty { get; } =
			DependencyProperty.Register("ItemsSource", typeof(object), typeof(ParallaxListView), new PropertyMetadata(null));

		#endregion

		#region ItemTemplateSelector Property

		public DataTemplateSelector ItemTemplateSelector
		{
			get { return (DataTemplateSelector)GetValue(ItemTemplateSelectorProperty); }
			set { SetValue(ItemTemplateSelectorProperty, value); }
		}

		public static DependencyProperty ItemTemplateSelectorProperty { get; } =
			DependencyProperty.Register("ItemTemplateSelector", typeof(DataTemplateSelector), typeof(ParallaxListView), new PropertyMetadata(null));

		#endregion

		#region ItemContainerStyle Property

		public Style ItemContainerStyle
		{
			get { return (Style)GetValue(ItemContainerStyleProperty); }
			set { SetValue(ItemContainerStyleProperty, value); }
		}

		public static DependencyProperty ItemContainerStyleProperty { get; } =
			DependencyProperty.Register("ItemContainerStyle", typeof(Style), typeof(ParallaxListView), new PropertyMetadata(null));

		#endregion

		#region ItemCommand Property

		public ICommand ItemCommand
		{
			get { return (ICommand)GetValue(ItemCommandProperty); }
			set { SetValue(ItemCommandProperty, value); }
		}

		public static DependencyProperty ItemCommandProperty { get; } =
			DependencyProperty.Register("ItemCommand", typeof(ICommand), typeof(ParallaxListView), new PropertyMetadata(null));

		#endregion

		#region Title Property

		public string Title
		{
			get { return (string)GetValue(TitleProperty); }
			set { SetValue(TitleProperty, value); }
		}

		public static DependencyProperty TitleProperty { get; } =
			DependencyProperty.Register("Title", typeof(string), typeof(ParallaxListView), new PropertyMetadata(string.Empty));

		#endregion

		#region VisibleTitle Property

		public string VisibleTitle
		{
			get { return (string)GetValue(VisibleTitleProperty); }
			set { SetValue(VisibleTitleProperty, value); }
		}

		public static DependencyProperty VisibleTitleProperty { get; } =
			DependencyProperty.Register("VisibleTitle", typeof(string), typeof(ParallaxListView), new PropertyMetadata("  "));

		#endregion

		#region HeaderBackgroundImage Property

		public string HeaderBackgroundImage
		{
			get { return (string)GetValue(HeaderBackgroundImageProperty); }
			set { SetValue(HeaderBackgroundImageProperty, value); }
		}

		public static DependencyProperty HeaderBackgroundImageProperty { get; } =
			DependencyProperty.Register("HeaderBackgroundImage", typeof(string), typeof(ParallaxListView), new PropertyMetadata(string.Empty));

		#endregion

		#region HeaderForegroundTemplate Property

		public DataTemplate HeaderForegroundTemplate
		{
			get { return (DataTemplate)GetValue(HeaderForegroundTemplateProperty); }
			set { SetValue(HeaderForegroundTemplateProperty, value); }
		}

		public static DependencyProperty HeaderForegroundTemplateProperty { get; } =
			DependencyProperty.Register("HeaderForegroundTemplate", typeof(DataTemplate), typeof(ParallaxListView), new PropertyMetadata(null));

		#endregion

		public ParallaxListView()
		{
			DefaultStyleKey = typeof(ParallaxListView);
			Loaded += OnLoaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			_screenHeight = XamlRoot!.Bounds.Height;

			var applied = _mainListview.ApplyTemplate();
			_mainScrollViewer = _mainListview.FindFirstChild<Microsoft.UI.Xaml.Controls.ScrollViewer>();
			var headerForegroundListView = this.GetTemplateChild(HeaderForegroundName) as Microsoft.UI.Xaml.Controls.ListView;
			var applied2 = headerForegroundListView.ApplyTemplate();
			_headerForeground = _headerForeground ?? headerForegroundListView.FindFirstChild<Microsoft.UI.Xaml.Controls.ScrollViewer>();

			_mainScrollViewer.ViewChanged -= UpdateHeaderPosition;
			_mainScrollViewer.ViewChanged += UpdateHeaderPosition;

			_mainScrollViewer.ViewChanged -= UpdateHeaderOpacity;
			_mainScrollViewer.ViewChanged += UpdateHeaderOpacity;

			_mainScrollViewer.ViewChanged -= UpdateCommandBarState;
			_mainScrollViewer.ViewChanged += UpdateCommandBarState;
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_headerBackground = this.GetTemplateChild(HeaderBackgroundName) as Microsoft.UI.Xaml.Controls.ScrollViewer;
			_mainListview = this.GetTemplateChild(ScrollViewerName) as Microsoft.UI.Xaml.Controls.ListView;
			_commandBar = this.GetTemplateChild(CommandBarName) as Microsoft.UI.Xaml.Controls.CommandBar;
		}

		private const bool DisableAnimation = true;
		private void UpdateHeaderPosition(object sender, ScrollViewerViewChangedEventArgs e)
		{
			var headerBackgroundOffset = _mainScrollViewer.VerticalOffset - (_mainScrollViewer.VerticalOffset / 1.5);
			var headerForegroundOffset = _mainScrollViewer.VerticalOffset - (_mainScrollViewer.VerticalOffset / 2);

			if (_mainScrollViewer.VerticalOffset < _screenHeight)
			{
				if (headerBackgroundOffset < _mainScrollViewer.VerticalOffset)
				{
					_headerBackground.ChangeView(0, headerBackgroundOffset, 1, DisableAnimation);
					_headerForeground.ChangeView(0, headerForegroundOffset, 1, DisableAnimation);
				}
				else
				{
					_headerBackground.ChangeView(0, 0, 1, DisableAnimation);
					_headerForeground.ChangeView(0, 0, 1, DisableAnimation);
				}
			}
		}

		private void UpdateHeaderOpacity(object sender, ScrollViewerViewChangedEventArgs e)
		{
			var opacity = 1 - (_mainScrollViewer.VerticalOffset / _screenHeight);
			_headerForeground.Opacity = opacity;
		}

		private void UpdateCommandBarState(object sender, ScrollViewerViewChangedEventArgs e)
		{
			if (_mainScrollViewer.VerticalOffset - TitleOffset > _screenHeight)
			{
				VisualStateManager.GoToState(this, "CommandBarAfterHeader", false);
				VisibleTitle = Title;
			}
			else if (_mainScrollViewer.VerticalOffset > _screenHeight - CommandBarHeight)
			{
				VisualStateManager.GoToState(this, "CommandBarOnHeader", false);
				VisibleTitle = "  ";
			}
			else
			{
				VisualStateManager.GoToState(this, "NormalState", false);
				VisibleTitle = "  ";
			}
		}
	}
}
