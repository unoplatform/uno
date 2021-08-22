/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetStyleDoubleParams
{
	/* Pack=4 */
	public HtmlId : string;
	public Name : string;
	public Value : number;
	public static unmarshal(pData:number) : WindowManagerSetStyleDoubleParams
	{
		const ret = new WindowManagerSetStyleDoubleParams();
		
		{
			const ptr = Module.getValue(pData + 0, "*");
			if(ptr !== 0)
			{
				ret.HtmlId = String(Module.UTF8ToString(ptr));
			}
			else
			
			{
				ret.HtmlId = null;
			}
		}
		
		{
			const ptr = Module.getValue(pData + 4, "*");
			if(ptr !== 0)
			{
				ret.Name = String(Module.UTF8ToString(ptr));
			}
			else
			
			{
				ret.Name = null;
			}
		}
		
		{
			ret.Value = Number(Module.getValue(pData + 8, "double"));
		}
		return ret;
	}
}
