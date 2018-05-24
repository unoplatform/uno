#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class CommandBar 
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.Collections.IObservableVector<global::Windows.UI.Xaml.Controls.ICommandBarElement> PrimaryCommands
		{
			get
			{
				return (global::Windows.Foundation.Collections.IObservableVector<global::Windows.UI.Xaml.Controls.ICommandBarElement>)this.GetValue(PrimaryCommandsProperty);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.Collections.IObservableVector<global::Windows.UI.Xaml.Controls.ICommandBarElement> SecondaryCommands
		{
			get
			{
				return (global::Windows.Foundation.Collections.IObservableVector<global::Windows.UI.Xaml.Controls.ICommandBarElement>)this.GetValue(SecondaryCommandsProperty);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Style CommandBarOverflowPresenterStyle
		{
			get
			{
				return (global::Windows.UI.Xaml.Style)this.GetValue(CommandBarOverflowPresenterStyleProperty);
			}
			set
			{
				this.SetValue(CommandBarOverflowPresenterStyleProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.Primitives.CommandBarTemplateSettings CommandBarTemplateSettings
		{
			get
			{
				throw new global::System.NotImplementedException("The member CommandBarTemplateSettings CommandBar.CommandBarTemplateSettings is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.CommandBarOverflowButtonVisibility OverflowButtonVisibility
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.CommandBarOverflowButtonVisibility)this.GetValue(OverflowButtonVisibilityProperty);
			}
			set
			{
				this.SetValue(OverflowButtonVisibilityProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool IsDynamicOverflowEnabled
		{
			get
			{
				return (bool)this.GetValue(IsDynamicOverflowEnabledProperty);
			}
			set
			{
				this.SetValue(IsDynamicOverflowEnabledProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.CommandBarDefaultLabelPosition DefaultLabelPosition
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.CommandBarDefaultLabelPosition)this.GetValue(DefaultLabelPositionProperty);
			}
			set
			{
				this.SetValue(DefaultLabelPositionProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty PrimaryCommandsProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"PrimaryCommands", typeof(global::Windows.Foundation.Collections.IObservableVector<global::Windows.UI.Xaml.Controls.ICommandBarElement>), 
			typeof(global::Windows.UI.Xaml.Controls.CommandBar), 
			new FrameworkPropertyMetadata(default(global::Windows.Foundation.Collections.IObservableVector<global::Windows.UI.Xaml.Controls.ICommandBarElement>)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty SecondaryCommandsProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"SecondaryCommands", typeof(global::Windows.Foundation.Collections.IObservableVector<global::Windows.UI.Xaml.Controls.ICommandBarElement>), 
			typeof(global::Windows.UI.Xaml.Controls.CommandBar), 
			new FrameworkPropertyMetadata(default(global::Windows.Foundation.Collections.IObservableVector<global::Windows.UI.Xaml.Controls.ICommandBarElement>)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty CommandBarOverflowPresenterStyleProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"CommandBarOverflowPresenterStyle", typeof(global::Windows.UI.Xaml.Style), 
			typeof(global::Windows.UI.Xaml.Controls.CommandBar), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Style)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty DefaultLabelPositionProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"DefaultLabelPosition", typeof(global::Windows.UI.Xaml.Controls.CommandBarDefaultLabelPosition), 
			typeof(global::Windows.UI.Xaml.Controls.CommandBar), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.CommandBarDefaultLabelPosition)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsDynamicOverflowEnabledProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsDynamicOverflowEnabled", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.CommandBar), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty OverflowButtonVisibilityProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"OverflowButtonVisibility", typeof(global::Windows.UI.Xaml.Controls.CommandBarOverflowButtonVisibility), 
			typeof(global::Windows.UI.Xaml.Controls.CommandBar), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.CommandBarOverflowButtonVisibility)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public CommandBar() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.CommandBar", "CommandBar.CommandBar()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.CommandBar.CommandBar()
		// Forced skipping of method Windows.UI.Xaml.Controls.CommandBar.PrimaryCommands.get
		// Forced skipping of method Windows.UI.Xaml.Controls.CommandBar.SecondaryCommands.get
		// Forced skipping of method Windows.UI.Xaml.Controls.CommandBar.CommandBarOverflowPresenterStyle.get
		// Forced skipping of method Windows.UI.Xaml.Controls.CommandBar.CommandBarOverflowPresenterStyle.set
		// Forced skipping of method Windows.UI.Xaml.Controls.CommandBar.CommandBarTemplateSettings.get
		// Forced skipping of method Windows.UI.Xaml.Controls.CommandBar.DefaultLabelPosition.get
		// Forced skipping of method Windows.UI.Xaml.Controls.CommandBar.DefaultLabelPosition.set
		// Forced skipping of method Windows.UI.Xaml.Controls.CommandBar.OverflowButtonVisibility.get
		// Forced skipping of method Windows.UI.Xaml.Controls.CommandBar.OverflowButtonVisibility.set
		// Forced skipping of method Windows.UI.Xaml.Controls.CommandBar.IsDynamicOverflowEnabled.get
		// Forced skipping of method Windows.UI.Xaml.Controls.CommandBar.IsDynamicOverflowEnabled.set
		// Forced skipping of method Windows.UI.Xaml.Controls.CommandBar.DynamicOverflowItemsChanging.add
		// Forced skipping of method Windows.UI.Xaml.Controls.CommandBar.DynamicOverflowItemsChanging.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.CommandBar.DefaultLabelPositionProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.CommandBar.OverflowButtonVisibilityProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.CommandBar.IsDynamicOverflowEnabledProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.CommandBar.CommandBarOverflowPresenterStyleProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.CommandBar.PrimaryCommandsProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.CommandBar.SecondaryCommandsProperty.get
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.CommandBar, global::Windows.UI.Xaml.Controls.DynamicOverflowItemsChangingEventArgs> DynamicOverflowItemsChanging
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.CommandBar", "event TypedEventHandler<CommandBar, DynamicOverflowItemsChangingEventArgs> CommandBar.DynamicOverflowItemsChanging");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.CommandBar", "event TypedEventHandler<CommandBar, DynamicOverflowItemsChangingEventArgs> CommandBar.DynamicOverflowItemsChanging");
			}
		}
		#endif
	}
}
