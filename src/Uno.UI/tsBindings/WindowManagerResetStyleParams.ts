/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerResetStyleParams
{
	/* Pack=4 */
	public HtmlId : string;
	public Styles_Length : number;
	public Styles : Array<string>;
	public static unmarshal(pData:number) : WindowManagerResetStyleParams
	{
		const ret = new WindowManagerResetStyleParams();
		
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
			ret.Styles_Length = Number(Module.getValue(pData + 4, "i32"));
		}
		
		{
			const pArray = Module.getValue(pData + 8, "*");
			if(pArray !== 0)
			{
				ret.Styles = new Array<string>();
				for(var i=0; i<ret.Styles_Length; i++)
				{
					const value = Module.getValue(pArray + i * 4, "*");
					if(value !== 0)
					{
						ret.Styles.push(String(MonoRuntime.conv_string(value)));
					}
					else
					
					{
						ret.Styles.push(null);
					}
				}
			}
			else
			
			{
				ret.Styles = null;
			}
		}
		return ret;
	}
}
