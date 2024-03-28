using Common;
using Uno.UITest;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace Windows.UI.Xaml.Tests.MUXControls.InteractionTests
{
	public class PipsPagerElements
	{
		private readonly IApp _app;

		public PipsPagerElements(IApp app)
		{
			_app = app;
		}

		private QueryEx PipsPager;
		private QueryEx NextPageButton;
		private QueryEx PreviousPageButton;
		private QueryEx RetrievePipsPagerButtonSizesButton;
		private QueryEx PreviousPageButtonVisibilityComboBox;
		private QueryEx NextPageButtonVisibilityComboBox;
		private QueryEx NumberOfPagesComboBox;
		private QueryEx MaxVisualIndicatorsComboBox;
		private QueryEx OrientationComboBox;
		private QueryEx PreviousPageButtonIsVisibleCheckBox;
		private QueryEx PreviousPageButtonIsEnabledCheckBox;
		private QueryEx NextPageButtonIsVisibleCheckBox;
		private QueryEx NextPageButtonIsEnabledCheckBox;
		private QueryEx CurrentPageTextBlock;
		private QueryEx FocusedPageIndexTextBlock;
		private QueryEx CurrentNumberOfPagesTextBlock;
		private QueryEx CurrentMaxVisualIndicatorsTextBlock;
		private QueryEx CurrentOrientationTextBlock;
		private QueryEx HorizontalOrientationPipsPagerButtonWidthTextBlock;
		private QueryEx HorizontalOrientationPipsPagerButtonHeightTextBlock;
		private QueryEx VerticalOrientationPipsPagerButtonWidthTextBlock;
		private QueryEx VerticalOrientationPipsPagerButtonHeightTextBlock;

		public QueryEx GetPipsPager()
		{
			return GetElement(ref PipsPager, "TestPipsPager");
		}
		public QueryEx GetPreviousPageButton()
		{
			return GetElementWithinPager(ref PreviousPageButton, "PreviousPageButton");
		}

		public QueryEx GetNextPageButton()
		{
			return GetElementWithinPager(ref NextPageButton, "NextPageButton");
		}

		public QueryEx GetPreviousPageButtonVisibilityComboBox()
		{
			return GetElement(ref PreviousPageButtonVisibilityComboBox, "PreviousPageButtonVisibilityComboBox");
		}

		public QueryEx GetNextPageButtonVisibilityComboBox()
		{
			return GetElement(ref NextPageButtonVisibilityComboBox, "NextPageButtonVisibilityComboBox");
		}

		public QueryEx GetPreviousPageButtonIsVisibleCheckBox()
		{
			return GetElement(ref PreviousPageButtonIsVisibleCheckBox, "PreviousPageButtonIsVisibleCheckBox");
		}
		public QueryEx GetPreviousPageButtonIsEnabledCheckBox()
		{
			return GetElement(ref PreviousPageButtonIsEnabledCheckBox, "PreviousPageButtonIsEnabledCheckBox");
		}
		public QueryEx GetNextPageButtonIsVisibleCheckBox()
		{
			return GetElement(ref NextPageButtonIsVisibleCheckBox, "NextPageButtonIsVisibleCheckBox");
		}
		public QueryEx GetNextPageButtonIsEnabledCheckBox()
		{
			return GetElement(ref NextPageButtonIsEnabledCheckBox, "NextPageButtonIsEnabledCheckBox");
		}

		public QueryEx GetNumberOfPagesComboBox()
		{
			return GetElement(ref NumberOfPagesComboBox, "TestPipsPagerNumberOfPagesComboBox");
		}

		public QueryEx GetMaxVisualIndicatorsComboBox()
		{
			return GetElement(ref MaxVisualIndicatorsComboBox, "TestPipsPagerMaxVisualIndicatorsComboBox");
		}
		public QueryEx GetOrientationComboBox()
		{
			return GetElement(ref OrientationComboBox, "TestPipsPagerOrientationComboBox");
		}
		public QueryEx GetCurrentPageTextBlock()
		{
			return GetElement(ref CurrentPageTextBlock, "CurrentPageIndexTextBlock");
		}

		public QueryEx GetFocusedPageIndexTextBlock()
		{
			return GetElement(ref FocusedPageIndexTextBlock, "FocusedPageIndexTextBlock");
		}

		public QueryEx GetCurrentMaxVisualIndicatorsTextBlock()
		{
			return GetElement(ref CurrentMaxVisualIndicatorsTextBlock, "CurrentMaxVisualIndicatorsTextBlock");
		}

		public QueryEx GetCurrentNumberOfPagesTextBlock()
		{
			return GetElement(ref CurrentNumberOfPagesTextBlock, "CurrentNumberOfPagesTextBlock");
		}

		public QueryEx GetCurrentOrientationTextBlock()
		{
			return GetElement(ref CurrentOrientationTextBlock, "CurrentOrientationTextBlock");
		}

		public QueryEx GetHorizontalOrientationPipsPagerButtonWidthTextBlock()
		{
			return GetElement(ref HorizontalOrientationPipsPagerButtonWidthTextBlock, "HorizontalOrientationPipsPagerButtonWidthTextBlock");
		}
		public QueryEx GetHorizontalOrientationPipsPagerButtonHeightTextBlock()
		{
			return GetElement(ref HorizontalOrientationPipsPagerButtonHeightTextBlock, "HorizontalOrientationPipsPagerButtonHeightTextBlock");
		}
		public QueryEx GetVerticalOrientationPipsPagerButtonWidthTextBlock()
		{
			return GetElement(ref VerticalOrientationPipsPagerButtonWidthTextBlock, "VerticalOrientationPipsPagerButtonWidthTextBlock");
		}
		public QueryEx GetVerticalOrientationPipsPagerButtonHeightTextBlock()
		{
			return GetElement(ref VerticalOrientationPipsPagerButtonHeightTextBlock, "VerticalOrientationPipsPagerButtonHeightTextBlock");
		}

		public QueryEx GetPipsPagerButtonSizesButton()
		{
			return GetElement(ref RetrievePipsPagerButtonSizesButton, "GetPipsPagersButtonSizesButton");
		}

		public QueryEx GetPipWithPageNumber(string elementName)
		{
			return _app.FindWithin(elementName, GetPipsPager());
		}
		private QueryEx GetElement(ref QueryEx element, string elementName)
		{
			if (element == null)
			{
				Log.Comment("Find the " + elementName);
				var result = _app.Marked(elementName);
				if (result != null && result.HasResults())
				{
					element = result;
				}
				Verify.IsNotNull(element);
				return result;
			}
			return element;
		}
		private QueryEx GetElementWithinPager(ref QueryEx element, string elementName)
		{
			if (element == null)
			{
				Log.Comment("Find the " + elementName);

				var pipsPager = GetPipsPager();
				var count = pipsPager.Results().Length;
				var result = _app.Marked(elementName);
				if (result != null && result.HasResults())
				{
					element = result;
				}
				//foreach (T child in GetPipsPager())
				//{
				//    if (child.AutomationId == elementName)
				//    {
				//        element = child;
				//        break;
				//    }
				//}
				Verify.IsNotNull(element);
			}
			return element;
		}
	}
}
