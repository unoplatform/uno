namespace Windows.UI.Notifications
{
	public partial class TileNotification
	{
		public Data.Xml.Dom.XmlDocument Content { get; }
		public TileNotification(Data.Xml.Dom.XmlDocument content)
		{
			Content = content;
		}
	}

}
