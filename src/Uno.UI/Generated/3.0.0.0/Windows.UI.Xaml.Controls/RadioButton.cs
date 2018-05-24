#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class RadioButton 
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  string GroupName
		{
			get
			{
				return (string)this.GetValue(GroupNameProperty);
			}
			set
			{
				this.SetValue(GroupNameProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty GroupNameProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"GroupName", typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.RadioButton), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public RadioButton() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.RadioButton", "RadioButton.RadioButton()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.RadioButton.RadioButton()
		// Forced skipping of method Windows.UI.Xaml.Controls.RadioButton.GroupName.get
		// Forced skipping of method Windows.UI.Xaml.Controls.RadioButton.GroupName.set
		// Forced skipping of method Windows.UI.Xaml.Controls.RadioButton.GroupNameProperty.get
	}
}
