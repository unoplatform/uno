#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls.Primitives
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IScrollSnapPointsInfo 
	{
		#if false || false || false || false
		bool AreHorizontalSnapPointsRegular
		{
			get;
		}
		#endif
		#if false || false || false || false
		bool AreVerticalSnapPointsRegular
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.IScrollSnapPointsInfo.AreHorizontalSnapPointsRegular.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.IScrollSnapPointsInfo.AreVerticalSnapPointsRegular.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.IScrollSnapPointsInfo.HorizontalSnapPointsChanged.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.IScrollSnapPointsInfo.HorizontalSnapPointsChanged.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.IScrollSnapPointsInfo.VerticalSnapPointsChanged.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.IScrollSnapPointsInfo.VerticalSnapPointsChanged.remove
		#if false || false || false || false
		global::System.Collections.Generic.IReadOnlyList<float> GetIrregularSnapPoints( global::Windows.UI.Xaml.Controls.Orientation orientation,  global::Windows.UI.Xaml.Controls.Primitives.SnapPointsAlignment alignment);
		#endif
		#if false || false || false || false
		float GetRegularSnapPoints( global::Windows.UI.Xaml.Controls.Orientation orientation,  global::Windows.UI.Xaml.Controls.Primitives.SnapPointsAlignment alignment, out float offset);
		#endif
		#if false || false || false || false
		 event global::System.EventHandler<object> HorizontalSnapPointsChanged;
		#endif
		#if false || false || false || false
		 event global::System.EventHandler<object> VerticalSnapPointsChanged;
		#endif
	}
}
