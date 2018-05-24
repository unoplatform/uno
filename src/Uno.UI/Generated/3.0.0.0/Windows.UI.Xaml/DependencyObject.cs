#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial interface DependencyObject 
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Core.CoreDispatcher Dispatcher
		{
			get
			{
				throw new global::System.NotImplementedException("The member CoreDispatcher DependencyObject.Dispatcher is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.DependencyObject.DependencyObject()
		#if false || false || false || false
		object GetValue( global::Windows.UI.Xaml.DependencyProperty dp);
		#endif
		#if false || false || false || false
		void SetValue( global::Windows.UI.Xaml.DependencyProperty dp,  object value);
		#endif
		#if false || false || false || false
		void ClearValue( global::Windows.UI.Xaml.DependencyProperty dp);
		#endif
		#if false || false || false || false
		object ReadLocalValue( global::Windows.UI.Xaml.DependencyProperty dp);
		#endif
		#if false || false || false || false
		object GetAnimationBaseValue( global::Windows.UI.Xaml.DependencyProperty dp);
		#endif
		// Forced skipping of method Windows.UI.Xaml.DependencyObject.Dispatcher.get
		#if false || false || false || false
		long RegisterPropertyChangedCallback( global::Windows.UI.Xaml.DependencyProperty dp,  global::Windows.UI.Xaml.DependencyPropertyChangedCallback callback);
		#endif
		#if false || false || false || false
		void UnregisterPropertyChangedCallback( global::Windows.UI.Xaml.DependencyProperty dp,  long token);
		#endif
	}
}
