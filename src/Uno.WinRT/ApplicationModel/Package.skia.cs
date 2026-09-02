namespace Windows.ApplicationModel;

public partial class Package
{
	private string _description = "";
	private string _publisherDisplayName = "";

	public string Description
	{
		get => EnsureLocalized(_description);
		private set => _description = value;
	}

	public string PublisherDisplayName
	{
		get => EnsureLocalized(_publisherDisplayName);
		private set => _publisherDisplayName = value;
	}
}
