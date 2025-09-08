using System;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class SelectionModelChildrenRequestedEventArgs
	{
		private object m_source;
		private IndexPath m_sourceIndexPath;
		// This flag allows for the re-use of a SelectionModelChildrenRequestedEventArgs object.
		// We do not want someone to cache the args object and access its properties later on, so we use this flag to only allow property access in the ChildrenRequested event handler.
		private bool m_throwOnAccess = true;

		internal SelectionModelChildrenRequestedEventArgs(object source, IndexPath sourceIndexPath, bool throwOnAccess)
		{
			Initialize(source, sourceIndexPath, throwOnAccess);
		}

		#region ISelectionModelChildrenRequestedEventArgs

		public object Source
		{
			get
			{
				if (m_throwOnAccess)
				{
					throw new InvalidOperationException("Source can only be accesed in the ChildrenRequested event handler.");
				}

				return m_source;
			}
		}

		public IndexPath SourceIndex
		{
			get
			{
				if (m_throwOnAccess)
				{
					throw new InvalidOperationException("SourceIndex can only be accesed in the ChildrenRequested event handler.");
				}

				return m_sourceIndexPath;
			}
		}

		public object Children { get; set; }

		#endregion

		internal void Initialize(object source, IndexPath sourceIndexPath, bool throwOnAccess)
		{
			m_source = source;
			m_sourceIndexPath = sourceIndexPath;
			m_throwOnAccess = throwOnAccess;
			Children = null;
		}
	}
}
