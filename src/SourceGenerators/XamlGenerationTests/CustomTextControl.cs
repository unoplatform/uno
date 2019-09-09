using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace XamlGenerationTests
{
	public partial class CustomTextControl : ContentControl
	{
		private TextBlock _customTextTextBlock;
		private TextBlock _customTextObjTextBlock;
		private TextBlock _customTextPlainTextBlock;
		private string _customTextPlain;

		public string CustomText
		{
			get { return (string)GetValue(CustomTextProperty); }
			set { SetValue(CustomTextProperty, value); }
		}

		// Using a DependencyProperty as the backing store for CustomText.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty CustomTextProperty =
			DependencyProperty.Register("CustomText", typeof(string), typeof(CustomTextControl), new PropertyMetadata(null, (o, __) => ((CustomTextControl)o).OnPropertyChanged()));

		public object CustomTextObj
		{
			get { return (object)GetValue(CustomTextObjProperty); }
			set { SetValue(CustomTextObjProperty, value); }
		}

		// Using a DependencyProperty as the backing store for CustomTextObj.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty CustomTextObjProperty =
			DependencyProperty.Register("CustomTextObj", typeof(object), typeof(CustomTextControl), new PropertyMetadata(null, (o, __) => ((CustomTextControl)o).OnPropertyChanged()));

		public string CustomTextPlain
		{
			get => _customTextPlain;
			set
			{
				_customTextPlain = value;
				OnPropertyChanged();
			}
		}


		private void OnPropertyChanged()
		{
			if (_customTextTextBlock == null)
			{
				InitializeContent();
			}

			_customTextTextBlock.Text = CustomText ?? "";
			_customTextObjTextBlock.Text = CustomTextObj?.ToString() ?? "";
			_customTextPlainTextBlock.Text = CustomTextPlain?.ToString() ?? "";
		}

		private void InitializeContent()
		{
			_customTextTextBlock = new TextBlock();
			_customTextObjTextBlock = new TextBlock();
			_customTextPlainTextBlock = new TextBlock();

			var panel = new StackPanel();
			panel.Children.Add(_customTextTextBlock);
			panel.Children.Add(_customTextObjTextBlock);
			panel.Children.Add(_customTextPlainTextBlock);

			Content = panel;
		}
	}
}
