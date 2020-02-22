using System;
using System.IO;

namespace Glory
{
	public class XbnfInclude :XbnfNode
	{
		public XbnfInclude(XbnfDocument document)
		{
			Document = document;
		}
		public XbnfInclude()
		{

		}
		
		public XbnfDocument Document { get; set; } = null;
		public override string ToString()
		{
			if (null == Document)
				return "";
			if(string.IsNullOrEmpty(Document.FileOrUrl))
			{
				return "@import <<in-memory>>;";
			}
			return "@import \"" + XbnfNode.Escape(Document.FileOrUrl) + "\";";
		}
	}
}
