/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerGetClientViewSizeReturn
{
	/* Pack=8 */
	public OffsetWidth : number;
	public OffsetHeight : number;
	public ClientWidth : number;
	public ClientHeight : number;
	public marshal(pData:number)
	{
		Module.setValue(pData + 0, this.OffsetWidth, "double");
		Module.setValue(pData + 8, this.OffsetHeight, "double");
		Module.setValue(pData + 16, this.ClientWidth, "double");
		Module.setValue(pData + 24, this.ClientHeight, "double");
	}
}
