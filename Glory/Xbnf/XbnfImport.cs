using System;
using System.IO;

namespace Glory
{
	public class XbnfImport :XbnfNode
	{
		public XbnfImport(XbnfDocument document)
		{
			Document = document;
		}
		public XbnfImport()
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
