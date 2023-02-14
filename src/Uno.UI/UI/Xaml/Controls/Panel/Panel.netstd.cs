using System.Collections;


namespace Windows.UI.Xaml.Controls;

partial class Panel : IEnumerable
{
	protected virtual void OnChildrenChanged()
	{
		UpdateBorder();
	}

	public new IEnumerator GetEnumerator() => this.GetChildren().GetEnumerator();

	partial void OnBackgroundChangedPartial(DependencyPropertyChangedEventArgs e) => UpdateHitTest();

	bool ICustomClippingElement.AllowClippingToLayoutSlot => true;

	bool ICustomClippingElement.ForceClippingToLayoutSlot => CornerRadiusInternal != CornerRadius.None;
}
