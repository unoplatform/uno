/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerRemoveViewParams
{
	/* Pack=4 */
	public HtmlId : number;
	public ChildView : number;
	public static unmarshal(pData:number) : WindowManagerRemoveViewParams
	{
		const ret = new WindowManagerRemoveViewParams();
		
		{
			ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
		}
		
		{
			ret.ChildView = Number(Module.getValue(pData + 4, "*"));
		}
		return ret;
	}
}
