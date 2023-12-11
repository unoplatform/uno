using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Extensions;
using Windows.UI.Xaml.Input;

namespace Uno.UI.RuntimeTests.Tests.HotReload.Frame;

internal static class UIElementExtensions
{
	public static Task ValidateTextOnChildTextBlock(this UIElement element, string expectedText, int index = 0)
		=> element.ValidateChildElement<TextBlock>(
			textBlock =>
			{
				Assert.AreEqual(expectedText, textBlock.Text);
				return Task.CompletedTask;
			},
			index);

	public static Task ValidateChildElement<TElement>(this UIElement element, Action<TElement> validation, int index = 0)
		where TElement : FrameworkElement
		=> element.ValidateChildElement<TElement>(
				selectedElement =>
				{
					validation(selectedElement);
					return Task.CompletedTask;
				},
				index);

	public static async Task ValidateChildElement<TElement>(this UIElement element, Func<TElement, Task> validation, int index = 0)
		where TElement : FrameworkElement
	{
		if (element is FrameworkElement fe)
		{
			await UnitTestsUIContentHelper.WaitForLoaded(fe);

			var selectedElement = element
				.EnumerateDescendants()
				.OfType<TElement>()
				.Skip(index)
				.FirstOrDefault();

			Assert.IsNotNull(selectedElement);
			await validation(selectedElement);
		}
	}

	public static async Task<(double VerticalOffset, double HorizontalOffset)> ScrollOffset(this UIElement element, int index = 0)
	{
		if (element is FrameworkElement fe)
		{
			await UnitTestsUIContentHelper.WaitForLoaded(fe);
			await UnitTestsUIContentHelper.WaitForIdle();

			var selectedElement = element
				.EnumerateDescendants()
				.OfType<ScrollViewer>()
				.Skip(index)
				.FirstOrDefault();

			Assert.IsNotNull(selectedElement);
			return (selectedElement.VerticalOffset, selectedElement.HorizontalOffset);
		}
		return default;
	}
}
