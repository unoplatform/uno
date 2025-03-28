// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

//  Abstract:
//      Contains common logic between the stacking and wrapping layout
//      strategies.

namespace Windows.UI.Xaml.Controls
{
	internal partial class LayoutStrategyBase
	{
		//public:

		public LayoutStrategyBase(bool useFullWidthHeaders, bool isWrapping)
		{
			m_useFullWidthHeaders = useFullWidthHeaders;
			m_isWrapping = isWrapping;
			m_pDataInfoProviderNoRef = null;
			m_virtualizationDirection = Orientation.Horizontal;
			m_groupHeaderStrategy = default;
			m_groupPadding = default;
		}

		public bool IsGrouping => m_groupHeaderStrategy != GroupHeaderStrategy.None;
		public bool IsWrappingStrategy => m_isWrapping;

		public Thickness GroupPadding
		{
			get => m_groupPadding;
			set => m_groupPadding = value;
		}

		public GroupHeaderStrategy GroupHeaderStrategy
		{
			get => m_groupHeaderStrategy;
			set => m_groupHeaderStrategy = value;
		}

		public Orientation VirtualizationDirection
		{
			get => m_virtualizationDirection;
			set => m_virtualizationDirection = value;
		}

		public bool UseFullWidthHeaders => m_useFullWidthHeaders;

		public virtual ILayoutDataInfoProvider GetLayoutDataInfoProviderNoRef
		{
			get => m_pDataInfoProviderNoRef;
			set => m_pDataInfoProviderNoRef = value;
		}

		// Used by unit tests only.
		public void SetUseFullWidthHeaders(bool useFullWidthHeaders) { m_useFullWidthHeaders = useFullWidthHeaders; }

		//protected:

		//    // a redirection to allow us to abstract the direction we care about
		//    // basically this will return a function to a member variable, switched on the orientation
		//    float wf.Point.* PointInNonVirtualizingDirection () ;
		//    float wf.Point.* PointInVirtualizingDirection () ;

		//    float wf.Size.* SizeInNonVirtualizingDirection () ;
		//    float wf.Size.* SizeInVirtualizingDirection () ;

		//    float wf.Rect.* PointFromRectInNonVirtualizingDirection () ;
		//    float wf.Rect.* PointFromRectInVirtualizingDirection () ;
		//    float wf.Rect.* SizeFromRectInNonVirtualizingDirection () ;
		//    float wf.Rect.* SizeFromRectInVirtualizingDirection () ;

		//    wf.Size GetGroupPaddingAtStart() ;
		//    wf.Size GetGroupPaddingAtEnd() ;

		//    // Determine if a point is inside the window, or is before or after it in the virtualizing direction.
		//    RelativePosition GetReferenceDirectionFromWindow( wf.Rect referenceRect,  wf.Rect window) ;

		//    static int GetRemainingGroups( int referenceGroupIndex,  int totalGroups,  RelativePosition positionOfReference);
		//    static int GetRemainingItems( int referenceItemIndex,  int totalItems,  RelativePosition positionOfReference);

		protected static int c_specialGroupIndex;
		protected static int c_specialItemIndex;

		//private:
		// direction in which we layout out items
		private Orientation m_virtualizationDirection;

		// extra space we surround every group with
		// When we give back a size for measure/arrange, we will include this padding around top & bottom.
		// then during container/header layout we will just ensure that a padding has been applied in between
		private Thickness m_groupPadding;

		private GroupHeaderStrategy m_groupHeaderStrategy;
		private bool m_isWrapping;
		private bool m_useFullWidthHeaders;

		ILayoutDataInfoProvider m_pDataInfoProviderNoRef;
	}
}
