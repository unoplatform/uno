using Android.Content;

namespace Uno.UI.RuntimeTests.Tests.NativeCtorGeneratorTests
{
	public partial class GenericClass<T1, T2> : Android.Views.View
	{
		public GenericClass(Context context) : base(context) { }
		public GenericClass(Context context, Android.Util.IAttributeSet attributeSet) : base(context, attributeSet) { }

		public partial class NestedGenericClass<T3> : Android.Views.View
		{
			public NestedGenericClass(Context context) : base(context) { }
			public NestedGenericClass(Context context, Android.Util.IAttributeSet attributeSet) : base(context, attributeSet) { }
		}
	}
}

public partial class GenericClassGlobalNamespace<T1, T2> : Android.Views.View
{
	public GenericClassGlobalNamespace(Context context) : base(context) { }
	public GenericClassGlobalNamespace(Context context, Android.Util.IAttributeSet attributeSet) : base(context, attributeSet) { }

	public partial class NestedGenericClassGlobalNamespace<T3> : Android.Views.View
	{
		public NestedGenericClassGlobalNamespace(Context context) : base(context) { }
		public NestedGenericClassGlobalNamespace(Context context, Android.Util.IAttributeSet attributeSet) : base(context, attributeSet) { }
	}
}
