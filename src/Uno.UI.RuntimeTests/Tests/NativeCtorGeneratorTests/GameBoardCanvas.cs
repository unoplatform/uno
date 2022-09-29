#nullable disable

#if __ANDROID__
using Android.Content;

namespace Uno.UI.RuntimeTests.Tests.NativeCtorGeneratorTests
{
	public partial class MyCustomView : Android.Views.View
	{
		public MyCustomView(Context context) : base(context) { }
		public MyCustomView(Context context, Android.Util.IAttributeSet attributeSet) : base(context, attributeSet) { }
	}

	public partial class GameBoardCanvas : MyCustomView
	{
		public GameBoardCanvas(Context context)
			: base(context)
		{
		}

		public GameBoardCanvas(Context context, Android.Util.IAttributeSet attributeSet)
			: base(context, attributeSet)
		{
		}
	}
}
#endif
