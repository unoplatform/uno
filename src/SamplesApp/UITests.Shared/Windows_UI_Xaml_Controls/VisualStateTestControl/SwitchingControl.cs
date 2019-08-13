using System.Windows.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.VisualStateTestControl
{
	[TemplateVisualState(GroupName = OnOffStateGroup, Name = OnState)]
	[TemplateVisualState(GroupName = OnOffStateGroup, Name = OffState)]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("nventive.Usage", "NV2002", Justification = "UI Toolkit")]
	public partial class SwitchingControl : Control
	{
		private const string OnOffStateGroup = "OnOff";
		private const string OnState = "On";
		private const string OffState = "Off";

		public SwitchingControl()
		{
			this.Switch = new Uno.UI.Common.DelegateCommand(() => IsOn = !IsOn);
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			UpdateVisualState(false);
		}

		public bool IsOn
		{
			get { return (bool)this.GetValue(IsOnProperty); }
			set { this.SetValue(IsOnProperty, value); }
		}

		// Using a DependencyProperty as the backing store for IsOn.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty IsOnProperty =
			DependencyProperty.Register("IsOn", typeof(bool), typeof(SwitchingControl), new PropertyMetadata(false, OnIsOnPropertyChanged));

		private static void OnIsOnPropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
		{
			var obj = sender as SwitchingControl;

			if (obj == null)
			{
				return;
			}

			obj.UpdateVisualState(true);
		}

		private void UpdateVisualState(bool useTransitions)
		{
			VisualStateManager.GoToState(this, this.IsOn ? OnState : OffState, useTransitions);
		}



		public ICommand Switch
		{
			get { return (ICommand)this.GetValue(SwitchProperty); }
			set { this.SetValue(SwitchProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Switch.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty SwitchProperty =
			DependencyProperty.Register("Switch", typeof(ICommand), typeof(SwitchingControl), new PropertyMetadata(default(ICommand)));
	}
}

