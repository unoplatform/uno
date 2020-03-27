/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerScrollToOptionsParams
{
	/* Pack=4 */
	public Left : number;
	public Top : number;
	public HasLeft : boolean;
	public HasTop : boolean;
	public DisableAnimation : boolean;
	public HtmlId : number;
	public static unmarshal(pData:number) : WindowManagerScrollToOptionsParams
	{
		const ret = new WindowManagerScrollToOptionsParams();
		
		{
			ret.Left = Number(Module.getValue(pData + 0, "double"));
		}
		
		{
			ret.Top = Number(Module.getValue(pData + 8, "double"));
		}
		
		{
			ret.HasLeft = Boolean(Module.getValue(pData + 16, "i32"));
		}
		
		{
			ret.HasTop = Boolean(Module.getValue(pData + 20, "i32"));
		}
		
		{
			ret.DisableAnimation = Boolean(Module.getValue(pData + 24, "i32"));
		}
		
		{
			ret.HtmlId = Number(Module.getValue(pData + 28, "*"));
		}
		return ret;
	}
}
