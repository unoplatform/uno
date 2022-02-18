/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetSelectionHighlightParams
{
	/* Pack=4 */
	public HtmlId : number;
	public BackgroundColor : number;
	public ForegroundColor : number;
	public static unmarshal(pData:number) : WindowManagerSetSelectionHighlightParams
	{
		const ret = new WindowManagerSetSelectionHighlightParams();
		
		{
			ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
		}
		
		{
			ret.BackgroundColor = Module.HEAPU32[(pData + 4) >> 2];
		}
		
		{
			ret.ForegroundColor = Module.HEAPU32[(pData + 8) >> 2];
		}
		return ret;
	}
}
