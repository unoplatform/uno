#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using System;

namespace Windows.UI.Xaml
{
    public partial class StateTrigger : global::Windows.UI.Xaml.StateTriggerBase
    {
        public bool IsActive
        {
            get { return (bool)this.GetValue(IsActiveProperty); }
            set { this.SetValue(IsActiveProperty, value); }
        }

        public static global::Windows.UI.Xaml.DependencyProperty IsActiveProperty { get; } =
        Windows.UI.Xaml.DependencyProperty.Register(
            "IsActive", typeof(bool),
            typeof(global::Windows.UI.Xaml.StateTrigger),
            new FrameworkPropertyMetadata(default(bool), propertyChangedCallback: (s, e) => (s as StateTrigger)?.OnIsActiveChanged(e)));

        private void OnIsActiveChanged(DependencyPropertyChangedEventArgs e)
        {
            if(e.NewValue is bool)
            {
                SetActive((bool)e.NewValue);
            }
        }

        public StateTrigger() : base()
        {

        }
    }
}
