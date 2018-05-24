#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class StateTrigger : global::Windows.UI.Xaml.StateTriggerBase
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool IsActive
		{
			get
			{
				return (bool)this.GetValue(IsActiveProperty);
			}
			set
			{
				this.SetValue(IsActiveProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsActiveProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsActive", typeof(bool), 
			typeof(global::Windows.UI.Xaml.StateTrigger), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public StateTrigger() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.StateTrigger", "StateTrigger.StateTrigger()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.StateTrigger.StateTrigger()
		// Forced skipping of method Windows.UI.Xaml.StateTrigger.IsActive.get
		// Forced skipping of method Windows.UI.Xaml.StateTrigger.IsActive.set
		// Forced skipping of method Windows.UI.Xaml.StateTrigger.IsActiveProperty.get
	}
}
