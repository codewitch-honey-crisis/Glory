using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glory
{
	struct XbnfGenerationInfo
	{
		public XbnfDocument Xbnf;
		public CfgDocument Cfg;
		public ListDictionary<XbnfExpression, string> TerminalMap;
		
	}
}
