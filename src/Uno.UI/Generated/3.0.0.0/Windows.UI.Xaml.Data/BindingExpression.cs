#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Data
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class BindingExpression : global::Windows.UI.Xaml.Data.BindingExpressionBase
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  object DataItem
		{
			get
			{
				throw new global::System.NotImplementedException("The member object BindingExpression.DataItem is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Data.Binding ParentBinding
		{
			get
			{
				throw new global::System.NotImplementedException("The member Binding BindingExpression.ParentBinding is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Data.BindingExpression.DataItem.get
		// Forced skipping of method Windows.UI.Xaml.Data.BindingExpression.ParentBinding.get
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  void UpdateSource()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Data.BindingExpression", "void BindingExpression.UpdateSource()");
		}
		#endif
	}
}
