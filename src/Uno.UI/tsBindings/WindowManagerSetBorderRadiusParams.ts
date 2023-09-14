/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetBorderRadiusParams
{
	/* Pack=8 */
	public HtmlId : number;
	public TopLeft : number;
	public TopRight : number;
	public BottomLeft : number;
	public BottomRight : number;
	public static unmarshal(pData:number) : WindowManagerSetBorderRadiusParams
	{
		const ret = new WindowManagerSetBorderRadiusParams();
		
		{
			ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
		}
		
		{
			ret.TopLeft = Number(Module.getValue(pData + 8, "double"));
		}
		
		{
			ret.TopRight = Number(Module.getValue(pData + 16, "double"));
		}
		
		{
			ret.BottomLeft = Number(Module.getValue(pData + 24, "double"));
		}
		
		{
			ret.BottomRight = Number(Module.getValue(pData + 32, "double"));
		}
		return ret;
	}
}
