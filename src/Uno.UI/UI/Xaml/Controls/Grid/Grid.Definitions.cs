namespace Windows.UI.Xaml.Controls
{
	partial class Grid
	{
		public Grid()
		{
		}

		public RowDefinitionCollection RowDefinitions
		{
			get
			{
				if (m_pRowDefinitions == null)
				{
					m_pRowDefinitions = new RowDefinitionCollection(this);
					m_pRowDefinitions.CollectionChanged += (snd, evt) => InvalidateDefinitions();
				}
				return m_pRowDefinitions;
			}
		}

		public ColumnDefinitionCollection ColumnDefinitions
		{
			get
			{

				if (m_pColumnDefinitions == null)
				{
					m_pColumnDefinitions = new ColumnDefinitionCollection(this);
					m_pColumnDefinitions.CollectionChanged += (snd, evt) => InvalidateDefinitions();
				}
				return m_pColumnDefinitions;
			}
		}
	}
}
