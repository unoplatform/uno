/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetSvgElementRectParams
{
	/* Pack=8 */
	public X : number;
	public Y : number;
	public Width : number;
	public Height : number;
	public HtmlId : number;
	public static unmarshal(pData:number) : WindowManagerSetSvgElementRectParams
	{
		const ret = new WindowManagerSetSvgElementRectParams();
		
		{
			ret.X = Number(Module.getValue(pData + 0, "double"));
		}
		
		{
			ret.Y = Number(Module.getValue(pData + 8, "double"));
		}
		
		{
			ret.Width = Number(Module.getValue(pData + 16, "double"));
		}
		
		{
			ret.Height = Number(Module.getValue(pData + 24, "double"));
		}
		
		{
			ret.HtmlId = Number(Module.getValue(pData + 32, "*"));
		}
		return ret;
	}
}
