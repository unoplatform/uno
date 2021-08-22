/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetClassesParams
{
	/* Pack=4 */
	public HtmlId : string;
	public CssClasses_Length : number;
	public CssClasses : Array<string>;
	public Index : number;
	public static unmarshal(pData:number) : WindowManagerSetClassesParams
	{
		const ret = new WindowManagerSetClassesParams();
		
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
			ret.CssClasses_Length = Number(Module.getValue(pData + 4, "i32"));
		}
		
		{
			const pArray = Module.getValue(pData + 8, "*");
			if(pArray !== 0)
			{
				ret.CssClasses = new Array<string>();
				for(var i=0; i<ret.CssClasses_Length; i++)
				{
					const value = Module.getValue(pArray + i * 4, "*");
					if(value !== 0)
					{
						ret.CssClasses.push(String(MonoRuntime.conv_string(value)));
					}
					else
					
					{
						ret.CssClasses.push(null);
					}
				}
			}
			else
			
			{
				ret.CssClasses = null;
			}
		}
		
		{
			ret.Index = Number(Module.getValue(pData + 12, "i32"));
		}
		return ret;
	}
}
