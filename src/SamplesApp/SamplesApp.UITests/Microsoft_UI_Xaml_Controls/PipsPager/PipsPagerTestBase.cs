using Common;
using SamplesApp.UITests;
using System;
using System.Linq;
using Uno.UITest.Helpers;

namespace Windows.UI.Xaml.Tests.MUXControls.InteractionTests
{
    public class PipsPagerTestBase : SampleControlUITestBase
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
            ToggleState isVisibleState;
            ToggleState isEnabledState;
            bool isHiddenOnEdge;

            if (btnType == ButtonType.Previous)
            {
                isVisibleState = elements.GetPreviousPageButtonIsVisibleCheckBox().ToggleState;
                isEnabledState = elements.GetPreviousPageButtonIsEnabledCheckBox().ToggleState;
                isHiddenOnEdge = GetCurrentPageAsInt32() == 0;
            }
            else
            {
                isVisibleState = elements.GetNextPageButtonIsVisibleCheckBox().ToggleState;
                isEnabledState = elements.GetNextPageButtonIsEnabledCheckBox().ToggleState;
                isHiddenOnEdge = !(GetCurrentPage() == "Infinite") && GetCurrentPageAsInt32() == GetSelectedNumberOfPagesAsInt32() - 1;
            }
            switch (modeSet)
            {
                case ButtonVisibilityMode.Collapsed:
                    Verify.AreEqual(isVisibleState, ToggleState.Off);
                    Verify.AreEqual(isEnabledState, ToggleState.Off);
                    break;

                case ButtonVisibilityMode.Visible:
                    if (isHiddenOnEdge)
                    {
                        Verify.AreEqual(isVisibleState, ToggleState.Off);
                        Verify.AreEqual(isEnabledState, ToggleState.Off);
                    }
                    else
                    {
                        Verify.AreEqual(isVisibleState, ToggleState.On);
                        Verify.AreEqual(isEnabledState, ToggleState.On);
                    }
                    break;

                case ButtonVisibilityMode.VisibleOnPointerOver:
                    if (isHiddenOnEdge)
                    {
                        Verify.AreEqual(isVisibleState, ToggleState.Off);
                        Verify.AreEqual(isEnabledState, ToggleState.Off);
                    }
                    else
                    {
                        if (isPagerInFocus)
                        {
                            Verify.AreEqual(isVisibleState, ToggleState.On);
                        }
                        else
                        {
                            Verify.AreEqual(isVisibleState, ToggleState.Off);
                        }
                        Verify.AreEqual(isEnabledState, ToggleState.On);
                    }
                    break;
            }
        }

        protected void VerifyNextPageButtonVisibility(string expected)
        {
            Verify.AreEqual(expected == Visibility.Visible, elements.GetNextPageButtonIsVisibleCheckBox().ToggleState == ToggleState.On);
        }

        protected void SetNextPageButtonVisibilityMode(ButtonVisibilityMode mode)
        {
            elements.GetNextPageButtonVisibilityComboBox().SelectItemByName($"{mode}NextButton");
        }

        protected void SetPreviousPageButtonVisibilityMode(ButtonVisibilityMode mode)
        {
            elements.GetPreviousPageButtonVisibilityComboBox().SelectItemByName($"{mode}PreviousButton");
        }

        protected void SetOrientation(string orientation)
        {			
            elements.GetOrientationComboBox().SelectItemByName($"{orientation}Orientation");
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
            elements.GetNumberOfPagesComboBox().SelectItemByName($"{numberOfPages}NumberOfPages");
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
    }
}
