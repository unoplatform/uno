using System;
using System.Linq;
using Common;
using SamplesApp.UITests;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace Windows.UI.Xaml.Tests.MUXControls.InteractionTests
{
	public partial class PipsPagerTestBase : SampleControlUITestBase
	{
		protected PipsPagerElements elements;
		protected delegate void SetButtonVisibilityModeFunction(ButtonVisibilityMode mode);
		protected delegate bool GetButtonIsHiddenOnEdgeFunction();

		protected void SelectPageInPager(int index)
		{
			_app.Tap("Page " + index.ToString());
			//Wait.ForIdle();
		}

		protected void VerifyPageButtonWithVisibilityModeSet(ButtonType btnType, ButtonVisibilityMode modeSet, bool isPagerInFocus = false)
		{
			string isVisibleState = null;
			string isEnabledState = null;
			bool isHiddenOnEdge;

			if (btnType == ButtonType.Previous)
			{
				isVisibleState = elements.GetPreviousPageButtonIsVisibleCheckBox().GetDependencyPropertyValue("IsChecked")?.ToString();
				isEnabledState = elements.GetPreviousPageButtonIsEnabledCheckBox().GetDependencyPropertyValue("IsChecked")?.ToString();
				isHiddenOnEdge = GetCurrentPageAsInt32() == 0;
			}
			else
			{
				isVisibleState = elements.GetNextPageButtonIsVisibleCheckBox().GetDependencyPropertyValue("IsChecked")?.ToString();
				isEnabledState = elements.GetNextPageButtonIsEnabledCheckBox().GetDependencyPropertyValue("IsChecked")?.ToString();
				isHiddenOnEdge = !(GetCurrentPage() == "Infinite") && GetCurrentPageAsInt32() == GetSelectedNumberOfPagesAsInt32() - 1;
			}
			switch (modeSet)
			{
				case ButtonVisibilityMode.Collapsed:
					Verify.AreEqual(isVisibleState, "False");
					Verify.AreEqual(isEnabledState, "False");
					break;

				case ButtonVisibilityMode.Visible:
					if (isHiddenOnEdge)
					{
						Verify.AreEqual(isVisibleState, "False");
						Verify.AreEqual(isEnabledState, "False");
					}
					else
					{
						Verify.AreEqual(isVisibleState, "True");
						Verify.AreEqual(isEnabledState, "True");
					}
					break;

				case ButtonVisibilityMode.VisibleOnPointerOver:
					if (isHiddenOnEdge)
					{
						Verify.AreEqual(isVisibleState, "False");
						Verify.AreEqual(isEnabledState, "False");
					}
					else
					{
						if (isPagerInFocus)
						{
							Verify.AreEqual(isVisibleState, "True");
						}
						else
						{
							Verify.AreEqual(isVisibleState, "False");
						}
						Verify.AreEqual(isEnabledState, "True");
					}
					break;
			}
		}

		protected void VerifyNextPageButtonVisibility(string expected)
		{
			//Verify.AreEqual(expected == Visibility.Visible, elements.GetNextPageButtonIsVisibleCheckBox().GetDependencyPropertyValue<bool?>("IsChecked") == ToggleState.On);
		}

		protected void SetNextPageButtonVisibilityMode(ButtonVisibilityMode mode)
		{
			elements.GetNextPageButtonVisibilityComboBox().SelectItemByIndex((int)mode);
		}

		protected void SetPreviousPageButtonVisibilityMode(ButtonVisibilityMode mode)
		{
			elements.GetPreviousPageButtonVisibilityComboBox().SelectItemByIndex((int)mode);
		}

		protected void SetOrientation(Orientation orientation)
		{
			elements.GetOrientationComboBox().SelectItemByIndex((int)orientation);
		}

		protected void VerifyOrientationChanged(string orientation)
		{
			Verify.AreEqual($"{orientation}", GetCurrentOrientation());
		}

		protected void VerifySelectedPageIndex(int expectedPage)
		{
			Verify.AreEqual(expectedPage, GetCurrentPageAsInt32());
		}

		protected void VerifyFocusedPageIndex(int index)
		{
			Verify.AreEqual(index, GetFocusedPageAsInt32());
		}

		protected void VerifySelectedFocusedIndex(int index)
		{
			VerifySelectedPageIndex(index);
			VerifyFocusedPageIndex(index);
		}

		protected string GetCurrentOrientation()
		{
			return elements.GetCurrentOrientationTextBlock().FirstResult().Text.Split(' ').Last();
		}

		protected int GetFocusedPageAsInt32()
		{
			return Convert.ToInt32(GetFocusedPage());
		}

		protected int GetCurrentPageAsInt32()
		{
			return Convert.ToInt32(GetCurrentPage());
		}

		protected string GetFocusedPage()
		{
			string focusedPageTextBlockContent = elements.GetFocusedPageIndexTextBlock().FirstResult().Text;
			return new string(focusedPageTextBlockContent.Where(char.IsDigit).ToArray());
		}

		protected string GetCurrentPage()
		{
			string currentPageTextBlockContent = elements.GetCurrentPageTextBlock().FirstResult().Text;
			return new string(currentPageTextBlockContent.Where(char.IsDigit).ToArray());
		}

		protected void ChangeNumberOfPages(NumberOfPagesOptions numberOfPages)
		{
			elements.GetNumberOfPagesComboBox().SelectItemByIndex((int)numberOfPages);
		}

		protected string GetSelectedNumberOfPages()
		{
			string selectedNumberOfPages = ExtractNumberFromString(elements.GetCurrentNumberOfPagesTextBlock().FirstResult().Text);
			if (string.IsNullOrEmpty(selectedNumberOfPages))
			{
				selectedNumberOfPages = "Infinite";
			}
			return selectedNumberOfPages;
		}

		protected int GetSelectedNumberOfPagesAsInt32()
		{
			return Convert.ToInt32(GetSelectedNumberOfPages());
		}
		protected void VerifyNumberOfPages(string numberOfPages)
		{
			Verify.AreEqual(numberOfPages, GetSelectedNumberOfPages());

		}

		private string ExtractNumberFromString(string text)
		{
			return new string(text.Where(char.IsDigit).ToArray());
		}

		public enum ButtonType
		{
			Previous,
			Next
		}

		public enum ButtonVisibilityMode
		{
			Visible,
			VisibleOnPointerOver,
			Collapsed
		}

		public enum NumberOfPagesOptions
		{
			Zero,
			Five,
			Ten,
			Twenty,
			Infinite
		}

		public enum Orientation
		{
			Vertical,
			Horizontal
		}
	}

	internal static class QueryExExtensions
	{
		public static void SelectItemByIndex(this QueryEx query, int index)
		{
			query.SetDependencyPropertyValue("SelectedIndex", index.ToString());
		}
	}
}
