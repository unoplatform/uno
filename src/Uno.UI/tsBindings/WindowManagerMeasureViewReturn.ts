/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerMeasureViewReturn
{
	/* Pack=8 */
	public DesiredWidth : number;
	public DesiredHeight : number;
	public marshal(pData:number)
	{
		Module.setValue(pData + 0, this.DesiredWidth, "double");
		Module.setValue(pData + 8, this.DesiredHeight, "double");
	}
}
