using Microsoft.UI.Xaml.Media;
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

partial class Panel
{
	private readonly BorderLayerRenderer _borderRenderer;

	public Panel()
	{
		_borderRenderer = new BorderLayerRenderer(this);

		Initialize();
	}

	partial void Initialize();

	partial void UpdateBorder() => _borderRenderer.Update();

	protected virtual void OnChildrenChanged() => UpdateBorder();

	partial void OnPaddingChangedPartial(Thickness oldValue, Thickness newValue) => UpdateBorder();

	partial void OnBorderBrushChangedPartial(Brush oldValue, Brush newValue) => UpdateBorder();

	partial void OnBorderThicknessChangedPartial(Thickness oldValue, Thickness newValue) => UpdateBorder();

	partial void OnCornerRadiusChangedPartial(CornerRadius oldValue, CornerRadius newValue) => UpdateBorder();

	/// <summary>        
	/// Support for the C# collection initializer style.
	/// Allows items to be added like this 
	/// new Panel 
	/// {
	///    new Border()
	/// }
	/// </summary>
	/// <param name="view"></param>
	public void Add(UIElement view)
	{
		Children.Add(view);
	}

	protected override void OnBackgroundChanged(DependencyPropertyChangedEventArgs e)
	{
		base.OnBackgroundChanged(e);
		UpdateBorder();
		UpdateHitTest();
	}
}
