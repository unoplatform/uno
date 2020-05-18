#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml
{
	#if false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class VisualStateManager : global::Windows.UI.Xaml.DependencyObject
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty CustomVisualStateManagerProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"CustomVisualStateManager", typeof(global::Windows.UI.Xaml.VisualStateManager), 
			typeof(global::Windows.UI.Xaml.VisualStateManager), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.VisualStateManager)));
		#endif
		// Skipping already declared method Windows.UI.Xaml.VisualStateManager.VisualStateManager()
		// Forced skipping of method Windows.UI.Xaml.VisualStateManager.VisualStateManager()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		protected  void RaiseCurrentStateChanging( global::Windows.UI.Xaml.VisualStateGroup stateGroup,  global::Windows.UI.Xaml.VisualState oldState,  global::Windows.UI.Xaml.VisualState newState,  global::Windows.UI.Xaml.Controls.Control control)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.VisualStateManager", "void VisualStateManager.RaiseCurrentStateChanging(VisualStateGroup stateGroup, VisualState oldState, VisualState newState, Control control)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		protected  void RaiseCurrentStateChanged( global::Windows.UI.Xaml.VisualStateGroup stateGroup,  global::Windows.UI.Xaml.VisualState oldState,  global::Windows.UI.Xaml.VisualState newState,  global::Windows.UI.Xaml.Controls.Control control)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.VisualStateManager", "void VisualStateManager.RaiseCurrentStateChanged(VisualStateGroup stateGroup, VisualState oldState, VisualState newState, Control control)");
		}
		#endif
		#if false || false || false || false || false
		[global::Uno.NotImplemented]
		protected virtual bool GoToStateCore( global::Windows.UI.Xaml.Controls.Control control,  global::Windows.UI.Xaml.FrameworkElement templateRoot,  string stateName,  global::Windows.UI.Xaml.VisualStateGroup group,  global::Windows.UI.Xaml.VisualState state,  bool useTransitions)
		{
			throw new global::System.NotImplementedException("The member bool VisualStateManager.GoToStateCore(Control control, FrameworkElement templateRoot, string stateName, VisualStateGroup group, VisualState state, bool useTransitions) is not implemented in Uno.");
		}
		#endif
		// Skipping already declared method Windows.UI.Xaml.VisualStateManager.GetVisualStateGroups(Windows.UI.Xaml.FrameworkElement)
		// Forced skipping of method Windows.UI.Xaml.VisualStateManager.CustomVisualStateManagerProperty.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.VisualStateManager GetCustomVisualStateManager( global::Windows.UI.Xaml.FrameworkElement obj)
		{
			return (global::Windows.UI.Xaml.VisualStateManager)obj.GetValue(CustomVisualStateManagerProperty);
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static void SetCustomVisualStateManager( global::Windows.UI.Xaml.FrameworkElement obj,  global::Windows.UI.Xaml.VisualStateManager value)
		{
			obj.SetValue(CustomVisualStateManagerProperty, value);
		}
		#endif
		// Skipping already declared method Windows.UI.Xaml.VisualStateManager.GoToState(Windows.UI.Xaml.Controls.Control, string, bool)
	}
}
