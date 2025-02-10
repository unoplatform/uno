using SampleControl.Entities;

public class SampleSearchResult
{
	private readonly SampleChooserContent _content;

	public SampleSearchResult(SampleChooserContent content)
	{
		_content = content;
	}

	public string Title => _content.ControlName;

	public override string ToString() => Title;
}
