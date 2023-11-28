/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetElementFillParams
{
	/* Pack=4 */
	public HtmlId : number;
	public Color : number;
	public static unmarshal(pData:number) : WindowManagerSetElementFillParams
	{
		const ret = new WindowManagerSetElementFillParams();
		
		{
			ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
		}
		
		{
			ret.Color = Module.HEAPU32[(pData + 4) >> 2];
		}
		return ret;
	}
}
