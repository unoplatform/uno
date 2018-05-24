namespace Windows.UI.Xaml.Controls
{
	internal sealed class DisplayMemberTemplateSelector : DataTemplateSelector
	{
		private readonly DataTemplate _dataTemplate;
		private readonly string _displayMemberPath;

		public DisplayMemberTemplateSelector(string displayMemberPath)
		{
			_displayMemberPath = displayMemberPath;
			_dataTemplate = new DataTemplate(() =>
				new TextBlock()
				.Binding("Text", displayMemberPath ?? string.Empty)
			);
		}

		protected override DataTemplate SelectTemplateCore(object item)
		{
			return SelectTemplateCore(item, null);
		}

		protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
		{
			return _dataTemplate;
		}

		public override int GetHashCode()
		{
			return _displayMemberPath.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			return (obj as DisplayMemberTemplateSelector)?._displayMemberPath == this._displayMemberPath;
		}
	}
}