using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Samples.Controls;
using Windows.Foundation;

namespace UITests.Shared.Windows_UI_Input.PointersTests
{
	[Sample("Pointers",Description = "This sample tests an issue where dragging on a TextBox selects text incorrectly. To ensures that dragging across the TextBox does not select text unless explicitly selected.")]
	public sealed partial class TextBox_Pointer : Page
	{
		public TextBox_Pointer()
		{
			this.InitializeComponent();
			SetupPointerEvents();
		}

		private void SetupPointerEvents()
		{
			TestTextBoxBorder.PointerPressed += OnPointerPressed;
			TestTextBoxBorder.PointerMoved += OnPointerMoved;
			TestTextBoxBorder.PointerReleased += OnPointerReleased;
		}

		private bool _isDragging;
		private Point? _initialPoint;

		private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
		{
			_initialPoint = e.GetCurrentPoint(TestTextBoxBorder).Position;
			_isDragging = false;
			ResultText.Text = "Pointer Pressed";
		}

		private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
		{
			if (_initialPoint is Point initialPoint)
			{
				var currentPoint = e.GetCurrentPoint(TestTextBoxBorder).Position;

				if (Math.Abs(currentPoint.X - initialPoint.X) > 2 || Math.Abs(currentPoint.Y - initialPoint.Y) > 2)
				{
					_isDragging = true;
					ResultText.Text = "Pointer Dragging";
				}
			}
		}

		private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
		{
			if (_isDragging)
			{
				var selectedText = TestTextBox.SelectedText;
				ResultText.Text = string.IsNullOrEmpty(selectedText)
					? "Pointer Released - No text selected"
					: $"Pointer Released - Selected Text: '{selectedText}'";
			}
			else
			{
				ResultText.Text = "Pointer Released - No drag detected";
			}

			_isDragging = false;
			_initialPoint = null;
		}
	}
}
