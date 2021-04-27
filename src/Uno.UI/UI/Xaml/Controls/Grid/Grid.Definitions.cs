namespace Windows.UI.Xaml.Controls
{
	partial class Grid
	{
		public Grid()
		{
			var rowDefinitions = new RowDefinitionCollection(this);
			rowDefinitions.CollectionChanged += (snd, evt) => InvalidateDefinitions();

			var columnDefinitions = new ColumnDefinitionCollection(this);
			columnDefinitions.CollectionChanged += (snd, evt) => InvalidateDefinitions();

			m_pRowDefinitions = rowDefinitions;
			m_pColumnDefinitions = columnDefinitions;
		}

		public RowDefinitionCollection RowDefinitions => m_pRowDefinitions;

		public ColumnDefinitionCollection ColumnDefinitions => m_pColumnDefinitions;
	}
}
