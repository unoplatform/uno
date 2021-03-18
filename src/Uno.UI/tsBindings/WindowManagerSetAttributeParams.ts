/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetAttributeParams
{
	/* Pack=4 */
	public HtmlId : number;
	public Name : string;
	public Value : string;
	public static unmarshal(pData:number) : WindowManagerSetAttributeParams
	{
		const ret = new WindowManagerSetAttributeParams();
		
		{
			ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
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
			const ptr = Module.getValue(pData + 8, "*");
			if(ptr !== 0)
			{
				ret.Value = String(Module.UTF8ToString(ptr));
			}
			else
			
			{
				ret.Value = null;
			}
		}
		return ret;
	}
}
