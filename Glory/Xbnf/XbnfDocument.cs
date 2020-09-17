using LC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Glory
{
	public class XbnfDocument : IEquatable<XbnfDocument>, ICloneable
	{
		string _fileOrUrl;
		public string FileOrUrl { get { return _fileOrUrl; } }
		public void SetFilename(string filename) { _fileOrUrl = filename; }
		public XbnfImportList Includes { get; } = new XbnfImportList();
		public XbnfOptionList Options { get; } = new XbnfOptionList();
		public XbnfProduction StartProduction {
			get {
				var ic = Productions.Count;
				var firstNT = -1;
				for (var i = 0;i<ic;++i)
				{
					var prod = Productions[i];
					var hi = prod.Attributes.IndexOf("start");
					if(-1<hi)
					{
						var o = prod.Attributes[hi].Value;
						if (o is bool && (bool)o)
							return prod;
					}
					if (-1 == firstNT && !prod.IsTerminal)
						firstNT = i;
				}
				if (-1!=firstNT)
					return Productions[firstNT];
				return null;
			}
			set {
				if (null!=value && !Productions.Contains(value))
					throw new InvalidOperationException(string.Concat("The production \"",value.Name,"\" is not the grammar."));
				for (int ic = Productions.Count, i = 0; i < ic; ++i)
				{
					if (null != value && Productions[i] == value)
					{
						var prod = Productions[i];
						prod.Attributes.Remove("start");
						prod.Attributes.Add(new XbnfAttribute("start", true));
					}
					else
					{
						var prod = Productions[i];
						var hi = prod.Attributes.IndexOf("start");
						if (-1 < hi)
							prod.Attributes.RemoveAt(hi);
					}
				}
			}
		}
		public static string[] GetResources(string fileOrUrl)
		{
			if (!fileOrUrl.Contains("://") && !Path.IsPathRooted(fileOrUrl))
				fileOrUrl = Path.GetFullPath(fileOrUrl);
			return _GatherIncludes(fileOrUrl);
		}
		static string[] _GatherIncludes(string res)
		{
			var result = new List<string>();
			var incs = new List<string>();
			result.Add(res);
			if (res.Contains("://"))
				using (var pc = LexContext.CreateFromUrl(res))
					_ParseIncludes(pc, result);
			else
			{
				using (var pc = LexContext.CreateFrom(res))
				{
					_ParseIncludes(pc, result);
					
				}
			}
			for(var i = 1;i<result.Count;++i)
			{
				var s = result[i];
				if(!s.Contains("://"))
				{
					if(!Path.IsPathRooted(s))
					{
						s=Path.Combine(Path.GetDirectoryName(res), s);
					} 
				}
				var gi = _GatherIncludes(s);
				for (var j = 0; j < gi.Length; j++)
					if (!result.Contains(gi[j]))
						result.Add(gi[j]);
			}
			for(int ic = result.Count,i=0;i<ic;++i)
			{
				if(!Path.IsPathRooted(result[i]))
				{
					result.RemoveAt(i);
					--i;
					--ic;
				}
			}
			return result.ToArray();
		}
		static void _ParseIncludes(LexContext pc,IList<string> result)
		{
			pc.TrySkipCCommentsAndWhiteSpace();
			while('@'==pc.Current)
			{
				pc.Advance();
				var s = XbnfNode.ParseIdentifier(pc);
				if("include"==s)
				{
					pc.TrySkipCCommentsAndWhiteSpace();
					var lit = XbnfExpression.Parse(pc) as XbnfLiteralExpression;
					if (!result.Contains(lit.Value))
						result.Add(lit.Value);
					pc.TryReadCCommentsAndWhitespace();
					pc.Advance();
					pc.TryReadCCommentsAndWhitespace();
				}
				else
				{
					while (-1 != pc.Current && ';' != pc.Current) pc.Advance();
					if (';' == pc.Current)
						pc.Advance();
					pc.TrySkipCCommentsAndWhiteSpace();
				}
			}
		}
		public IList<XbnfCode> Code { get; } = new List<XbnfCode>();

		public bool HasNonTerminalProductions {
			get {
				for (int ic = Productions.Count, i = 0; i < ic; ++i)
					if (!Productions[i].IsTerminal)
						return true;
				return false;
			}
		}
		static XbnfExpression _FindNonTerminal(XbnfExpression expr)
		{
			var re = expr as XbnfRefExpression;
			if(null!=re)
				return re;
			var bo = expr as XbnfBinaryExpression;
			if(bo!=null)
			{
				var res = _FindNonTerminal(bo.Left);
				if (null != res)
					return res;
				res = _FindNonTerminal(bo.Right);
				if (null != res)
					return res;
			}
			var ue = expr as XbnfUnaryExpression;
			if(null!=ue)
				return _FindNonTerminal(ue.Expression);
			return null;
		}
		public XbnfProductionList Productions { get; } = new XbnfProductionList();
		public IList<XbnfMessage> TryValidate(IList<XbnfMessage> result = null)
		{
			if (null == result)
				result = new List<XbnfMessage>();
			var refCounts = new Dictionary<string, int>(EqualityComparer<string>.Default);

			foreach (var prod in Productions)
			{
				if (refCounts.ContainsKey(prod.Name))
					result.Add(new XbnfMessage(ErrorLevel.Error, -1, string.Concat("The production \"", prod.Name, "\" was specified more than once."), prod.Line, prod.Column, prod.Position, FileOrUrl));
				else
					refCounts.Add(prod.Name, 0);
			}
			foreach (var prod in Productions)
			{
				_ValidateExpression(prod.Expression, refCounts, result);
			}
			foreach (var rc in refCounts)
			{
				if (0 == rc.Value)
				{
					var prod = Productions[rc.Key];
					object o;
					var i = prod.Attributes.IndexOf("hidden");
					var isHidden = false;
					if (-1<i)
					{
						o = prod.Attributes[i].Value;
						isHidden = (o is bool && (bool)o);
					}
					var sp = StartProduction;
					if(null!=sp)
						if (!isHidden && !Equals(rc.Key, sp.Name))
							result.Add(new XbnfMessage(ErrorLevel.Warning, -1, string.Concat("Unreferenced production \"", prod.Name, "\""),
								prod.Line, prod.Column, prod.Position, FileOrUrl));
				}
			}
			return result;
		}
		public XbnfDocument Clone()
		{
			var result = new XbnfDocument();
			for(int ic=Options.Count,i=0;i<ic;++i)
				result.Options.Add(Options[i].Clone());
			for (int ic = Includes.Count, i = 0; i < ic; ++i)
				result.Includes.Add(new XbnfImport(Includes[i].Document));
			for (int ic=Productions.Count,i=0;i<ic;++i)
				result.Productions.Add(Productions[i].Clone());
			return result;
		}
		object ICloneable.Clone()
			=> Clone();
		public string ToString(string fmt)
		{
			var sb = new StringBuilder();
			for(int ic=Includes.Count,i=0;i<ic;++i)
			{
				sb.Append("@include ");
				sb.Append("\""+XbnfNode.Escape(Includes[i].Document.FileOrUrl)+"\"");
				sb.AppendLine(";");
			}
			var oc = Options.Count;
			if(0<oc)
			{
				sb.Append("@options ");
				for (var i = 0; i < oc; ++i)
				{
					if (0 != i)
						sb.Append(", ");
					sb.Append(Options[i]);					
				}
				sb.AppendLine(";");
			}
			if ("gnc" == fmt)
				for (int ic = Productions.Count, i = 0; i < ic; ++i)
					sb.AppendLine(Productions[i].ToString("pnc"));
			else if ("xc" == fmt)
				for (int ic = Productions.Count, i = 0; i < ic; ++i)
					sb.AppendLine(Productions[i].ToString("xc"));
			else
				for (int ic = Productions.Count, i = 0; i < ic; ++i)
					sb.AppendLine(Productions[i].ToString());
			return sb.ToString();
		}
		public override string ToString()
		{
			return ToString(null);
		}

		internal static XbnfDocument Parse(LexContext pc)
		{
			var result = new XbnfDocument();
			if(!string.IsNullOrEmpty(pc.FileOrUrl))
				result.SetFilename(pc.FileOrUrl);
			while (-1 != pc.Current && '}'!=pc.Current)
			{
				pc.TrySkipCCommentsAndWhiteSpace();
				while ('@' == pc.Current) // directives
				{
					pc.Advance();
					var l = pc.Line;
					var c = pc.Column;
					var p = pc.Position;
					var str = XbnfNode.ParseIdentifier(pc);
					if (0 == string.Compare("include", str, StringComparison.InvariantCulture))
					{
						result.Includes.Add(_ParseIncludePart(result, pc));
					} else if(0==string.Compare("options",str,StringComparison.InvariantCulture))
					{
						if(0<result.Options.Count)
						{
							throw new ExpectingException("Duplicate directive \"options\" specified", l, c, p,pc.FileOrUrl);
						}
						while (-1 != pc.Current && ';' != pc.Current)
						{
							l = pc.Line;
							c = pc.Column;
							p = pc.Position;
							var opt = XbnfOption.Parse(pc);
							opt.SetLocation(l, c, p);
							result.Options.Add(opt);
							pc.TrySkipCCommentsAndWhiteSpace();
							pc.Expecting(';', ',');
							if (',' == pc.Current)
								pc.Advance();
						}
						pc.Expecting(';');
						pc.Advance();
						if(0==result.Options.Count)
						{
							throw new ExpectingException("Expection options but \"options\" directive was empty", l, c, p,pc.FileOrUrl);
						}
					}
					else
					{
						throw new ExpectingException("Expecting \"include\" or \"options\"", l, c, p,pc.FileOrUrl,"include","options");
					}
					

					pc.TrySkipCCommentsAndWhiteSpace();
				}
				if (pc.Current == '{')
				{
					pc.Advance();
					var l = pc.Line;
					var c = pc.Column;
					var p = pc.Position;
					var s = ReadCode(pc);
					pc.Expecting('}');
					pc.Advance();
					var code = new XbnfCode(s);
					code.SetLocation(l, c, p);
					result.Code.Add(code);
				}
				else if (-1 != pc.Current)
				{
					if ('@' == pc.Current)
					{
						throw new ExpectingException("Expecting productions. Includes and options must be specified before any productions", pc.Line, pc.Column, pc.Position, pc.FileOrUrl,"Production");
					}
					result.Productions.Add(XbnfProduction.Parse(pc));
				}
				else // end of input
					return result;
				// have to do this so trailing whitespace
				// doesn't get read as a production
				pc.TryReadCCommentsAndWhitespace();
			} 
			return result;
		}
		public static XbnfDocument Parse(IEnumerable<char> @string)
			=> Parse(LexContext.Create(@string));
		public static XbnfDocument ReadFrom(TextReader reader)
			=> Parse(LexContext.CreateFrom(reader));
		public static XbnfDocument ReadFrom(string file)
		{
			using (var pc = LexContext.CreateFrom(file))
			{
				var result = Parse(pc);
				result._fileOrUrl = Path.GetFullPath(file);
				return result;
			}
		}
		public static XbnfDocument ReadFromUrl(string url)
		{
			using (var pc = LexContext.CreateFromUrl(url))
				return Parse(pc);
		}
		static internal string ReadCode(LexContext pc)
		{
			var sb = new StringBuilder();
			var i = 1;
			var skipRead = true;
			while (skipRead || -1 != pc.Advance())
			{
				skipRead = false;
				if ('{' == pc.Current)
				{
					sb.Append((char)pc.Current);
					++i;
				}
				else if ('}' == pc.Current)
				{
					--i;
					if (0 == i)
						break;
					sb.Append((char)pc.Current);
				}
				else if ('\"' == pc.Current)
				{
					pc.ClearCapture();
					pc.TryReadCString();
					sb.Append(pc.GetCapture());
					skipRead = true;
				}
				else
					sb.Append((char)pc.Current);
				pc.ClearCapture();
				if (pc.TryReadCCommentsAndWhitespace())
					skipRead = true;
				sb.Append(pc.GetCapture());

			}

			return sb.ToString();
		}
		#region Value semantics
		public bool Equals(XbnfDocument rhs)
		{
			if (ReferenceEquals(rhs, this)) return true;
			if (ReferenceEquals(rhs, null)) return false;
			var lc = Productions.Count;
			var rc = rhs.Productions.Count;
			if (lc != rc) return false;
			for (var i=0;i<lc;++i)
				if (Productions[i] != rhs.Productions[i])
					return false;
			
			return true;
		}
		public override bool Equals(object rhs)
			=> Equals(rhs as XbnfDocument);

		public override int GetHashCode()
		{
			var result = 0;
			for(int ic=Productions.Count,i=0;i<ic;++i)
				if (null != Productions[i])
					result ^=Productions[i].GetHashCode();
			
			return result;
		}
		public static bool operator==(XbnfDocument lhs, XbnfDocument rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return true;
			if (ReferenceEquals(lhs, null)) return false;
			return lhs.Equals(rhs);
		}
		public static bool operator !=(XbnfDocument lhs, XbnfDocument rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return false;
			if (ReferenceEquals(lhs, null)) return true;
			return !lhs.Equals(rhs);
		}
		#endregion
		void _ValidateExpression(XbnfExpression expr, IDictionary<string, int> refCounts, IList<XbnfMessage> messages)
		{
			var l = expr as XbnfLiteralExpression;
			if (null != l)
			{

				string id = null;
				for(int ic = Productions.Count,i=0;i<ic;++i)
				{
					var ll = Productions[i].Expression as XbnfLiteralExpression;
					if(ll==l)
					{
						id = Productions[i].Name;
						break;
					}
				}
				// don't count itself. only things just like itself
				if (!string.IsNullOrEmpty(id) && !ReferenceEquals(Productions[id].Expression, l))
					refCounts[id] += 1;
			}
			
			var r = expr as XbnfRefExpression;
			if (null != r)
			{
				int rc;
				if (null == r.Symbol)
				{
					messages.Add(
						new XbnfMessage(
							ErrorLevel.Error, -1,
							"Null reference expression",
							expr.Line, expr.Column, expr.Position, FileOrUrl));
					return;
				}
				if (!refCounts.TryGetValue(r.Symbol, out rc))
				{
					messages.Add(
						new XbnfMessage(
							ErrorLevel.Error, -1,
							string.Concat(
								"Reference to undefined symbol \"",
								r.Symbol,
								"\""),
							expr.Line, expr.Column, expr.Position, FileOrUrl));
					return;
				}
				refCounts[r.Symbol] = rc + 1;
				return;
			}
			var b = expr as XbnfBinaryExpression;
			if (null != b)
			{
				if (null == b.Left && null == b.Right)
				{
					messages.Add(
						new XbnfMessage(
							ErrorLevel.Warning, -1,
								"Nil expression",
							expr.Line, expr.Column, expr.Position, FileOrUrl));
					return;
				}
				_ValidateExpression(b.Left, refCounts, messages);
				_ValidateExpression(b.Right, refCounts, messages);
				return;
			}
			var u = expr as XbnfUnaryExpression;
			if (null != u)
			{
				if (null == u.Expression)
				{
					messages.Add(
						new XbnfMessage(
							ErrorLevel.Warning, -1,
								"Nil expression",
							expr.Line, expr.Column, expr.Position, FileOrUrl));
					return;
				}
				_ValidateExpression(u.Expression, refCounts, messages);
			}
		}
		public XbnfProduction GetProductionForExpression(XbnfExpression expr)
		{
			for (int ic = Productions.Count, i = 0; i < ic; ++i)
			{
				var prod = Productions[i];
				if (Equals(expr , prod.Expression))
					return prod;
			}
			return null;
		}
		public static XbnfDocument Merge(IEnumerable<XbnfDocument> documents)
		{
			var result = new XbnfDocument();
			var first = true;
			string ss=null;
			foreach(var doc in documents)
			{
				if(first)
				{
					ss = doc.StartProduction?.Name;
					foreach(var opt in doc.Options)
					{
						result.Options.Add(opt);
					}
					first = false;
				}
				foreach(var inc in doc.Includes)
				{
					result.Includes.Add(inc);	
				}
				foreach(var prod in doc.Productions)
				{
					if(result.Productions.Contains(prod.Name))
						throw new InvalidOperationException(string.Format("A duplicate production named \"{0}\" was found in the grammar at line {1}, column {2}, position {3} in {4}", prod.Name, prod.Line, prod.Column, prod.Position, doc.FileOrUrl));
					result.Productions.Add(prod);
				}
				foreach(var code in doc.Code)
				{
					result.Code.Add(code);
				}
			}
			if(null!=ss)
				result.StartProduction = result.Productions[ss];
			return result;
		}
		static XbnfImport _ParseIncludePart(XbnfDocument doc, LexContext pc)
		{
			pc.TrySkipCCommentsAndWhiteSpace();
			var l = pc.Line;
			var c = pc.Column;
			var p = pc.Position;
			pc.Expecting('\"');
			// borrow the parsing from XbnfExpression for this.
			var le = XbnfExpression.Parse(pc) as XbnfLiteralExpression;
			if (null == le)
			{
				throw new ExpectingException("Expecting string literal include argument", l, c, p,pc.FileOrUrl,"string literal");
			}
			var res = le.Value;
			pc.TrySkipCCommentsAndWhiteSpace();
			pc.Expecting(';');
			pc.Advance();
			var cmp = res.ToLowerInvariant();
			var result = new XbnfImport();
			if (-1 < cmp.IndexOf("://"))
				result.Document = XbnfDocument.ReadFromUrl(cmp);
			else
			{
				string mdir = null;
				if (null != doc && !string.IsNullOrEmpty(doc.FileOrUrl))
				{
					mdir = doc.FileOrUrl;
					if (!Path.IsPathRooted(mdir))
						mdir = Path.GetFullPath(mdir);
					mdir = Path.GetDirectoryName(mdir);
				}
				var path = res;
				if (!Path.IsPathRooted(path))
				{
					if (null != mdir)
						path = Path.Combine(mdir, path);
					else
						path = Path.GetFullPath(path);
				}
				result.Document = XbnfDocument.ReadFrom(path);
			}
			result.SetLocation(l, c, p);
			return result;
		}
	}
}
