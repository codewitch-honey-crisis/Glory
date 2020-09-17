using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Glory
{
	// port of PCK's XbnkToPckTransform for use with Rolex and Parsley
	// the rolex spec is written to rolexOutput
	// the CfgDocument is the return value
	using TermPriEntry = KeyValuePair<string, int>;
	static class XbnfConvert
	{
		public static List<string> GetSymbolTable(XbnfGenerationInfo info, out int termStart)
		{
			var result = new List<string>();
			var seen = new HashSet<string>();
			XbnfDocument xbnf = info.Xbnf;
			var cfg = info.Cfg;
			
			foreach (var s in cfg.FillNonTerminals())
			{
				if (seen.Add(s))
					result.Add(s);
			}
			
			termStart = result.Count;
			var ts = new List<string>();
			foreach (var prod in xbnf.Productions)
			{
				if (prod.IsTerminal)
				{
					ts.Add(prod.Name);
				}
			}
			foreach (var s in ts)
			{
				//if (0 != string.Compare("#ERROR", s, StringComparison.InvariantCulture) && 0 != string.Compare("#EOS", s, StringComparison.InvariantCulture))
				//{
				if (seen.Add(s))
					result.Add(s);
				//}
			}
			
			result.Add("#EOS");
			result.Add("#ERROR");
			return result;
		}

		private class _TermPriorityComparer : IComparer<TermPriEntry>
		{
			CfgDocument _cfg;
			public _TermPriorityComparer(CfgDocument cfg) { _cfg = cfg; }
			public int Compare(TermPriEntry x, TermPriEntry y)
			{
				var px = _FindPriority(x.Key) * 0x100000000 + x.Value;
				// aloha
				var py = _FindPriority(y.Key) * 0x100000000 + y.Value;

				var c = px - py;
				if (0 > c)
					return -1;
				if (0 < c)
					return 1;
				return 0;
			}

			int _FindPriority(string x)
			{
				// not working?!
				if ("#ERROR" == x || "#EOS" == x)
					return int.MaxValue;
				
				var o = _cfg.GetAttribute(x, "priority");
				if (o is double)
				{
					return -(int)(double)o;
				}
				

				return 0;
			}
		}

		public static string ToRolexSpec(XbnfGenerationInfo genInfo)
		{

			var termStart = 0;
			var stbl = GetSymbolTable(genInfo, out termStart);
			var stbli = new List<KeyValuePair<string, int>>();
			var id = 0;

			// assign temp ids
			foreach (var se in stbl)
			{
				if (id >= termStart)
				{
					stbli.Add(new KeyValuePair<string, int>(se, id));
				}
				++id;
			}
			stbli.Sort(new _TermPriorityComparer(genInfo.Cfg));

			var sb = new StringBuilder();
			for (int ic = stbli.Count, i = 0; i < ic; ++i)
			{
				var sym = stbli[i].Key;
				if ("#EOS" == sym|| "#ERROR" == sym)
					continue;
				XbnfExpression e = null;
				foreach (var k in genInfo.TerminalMap)
				{
					if (k.Value == sym)
					{
						e = k.Key;
						break;
					}
				}
				//var te = genInfo.TerminalMap[i];
				//var sym = te.Value;
				//var id = stbli.IndexOf(new KeyValuePair<string, XbnfDocument>(sym, d));
				id = stbli[i].Value;
				if (-1 < id) // some terminals might never be used.
				{
					// implicit terminals do not have productions and therefore attributes
					var pi = genInfo.Xbnf.Productions.IndexOf(sym);
					if (-1 < pi)
					{
						// explicit
						var prod = genInfo.Xbnf.Productions[pi];
						sb.Append(sym);
						sb.Append("<id=");
						sb.Append(id);
						foreach (var attr in prod.Attributes)
						{
							sb.Append(", ");
							sb.Append(attr.ToString());
						}
						sb.Append(">");
					}
					else
					{
						// implicit
						sb.Append(sym);
						sb.Append(string.Concat("<id=", id, ">"));
					}
					sb.AppendLine(string.Concat("= \'", _ToRegex(genInfo.Xbnf, e, true), "\'"));
				}
			}
			return sb.ToString();
		}

		public static IList<IMessage> TryCreateGenerationInfo(XbnfDocument document, out XbnfGenerationInfo genInfo)
		{
			var includes = new XbnfImportList();
			_GatherIncludes(document, includes);
			var incs = new List<XbnfDocument>();
			incs.Add(document);
			foreach (var inc in includes)
				incs.Add(inc.Document);
			var doc = XbnfDocument.Merge(incs);
			var cfg = new CfgDocument();

			return _TryToGenInfo(doc, cfg, out genInfo);
		}
		static void _GatherIncludes(XbnfDocument doc, XbnfImportList result)
		{
			for (int ic = doc.Includes.Count, i = 0; i < ic; ++i)
			{
				var inc = doc.Includes[i];
				var found = false;
				for (int jc = result.Count, j = 0; j < jc; ++j)
				{
					var fn = result[i].Document.FileOrUrl;
					if (!string.IsNullOrEmpty(fn) && 0 == string.Compare(fn, inc.Document.FileOrUrl))
					{
						found = true;
						break;
					}
				}
				if (!found)
				{
					result.Add(inc);
					_GatherIncludes(inc.Document, result);
				}
			}
		}

		static IList<IMessage> _TryToGenInfo(XbnfDocument document, CfgDocument cfg, out XbnfGenerationInfo genInfo)
		{
			genInfo = default(XbnfGenerationInfo);
			var hasErrors = false;
			var result = new List<IMessage>();
			var syms = new HashSet<string>();
			// gather the attributes and production names

			for (int ic = document.Productions.Count, i = 0; i < ic; ++i)
			{
				var p = document.Productions[i];
				if (!syms.Add(p.Name))
				{
					result.Add(new XbnfMessage(ErrorLevel.Error, -1, string.Format("Duplicate production {0} defined.", p.Name), p.Line, p.Column, p.Position, document.FileOrUrl));
					hasErrors = true;
				}
				if (0 < p.Attributes.Count)
				{
					CfgAttributeList attrs;
					if (!cfg.AttributeSets.TryGetValue(p.Name, out attrs))
					{
						attrs = new CfgAttributeList();
						cfg.AttributeSets.Add(p.Name, attrs);
					}
					for (int jc = p.Attributes.Count, j = 0; j < jc; ++j)
					{
						var attr = p.Attributes[j];
						attrs.Add(new CfgAttribute(attr.Name, attr.Value));
					}
				}
			}

			// use a list dictionary to keep these in order
			var tmap = new ListDictionary<XbnfExpression, string>();
			var attrSets = new Dictionary<string, XbnfAttributeList>();
			var rules = new List<KeyValuePair<string, IList<string>>>();
			// below are scratch
			var working = new HashSet<XbnfExpression>();
			var done = new HashSet<XbnfExpression>();
			// now get the terminals and their ids, declaring if necessary


			for (int ic = document.Productions.Count, i = 0; i < ic; ++i)
			{

				var p = document.Productions[i];
				if (p.IsTerminal)
				{
					string name;
					if (!tmap.TryGetValue(p.Expression, out name))
					{
						tmap.Add(p.Expression, p.Name);

					}
					else
					{
						if (name != p.Name)
						{
							result.Add(new CfgMessage(ErrorLevel.Error, -1, string.Format("{0} attempts to redefine terminal {1}", name, p.Name), p.Line, p.Column, p.Position, document.FileOrUrl));
							hasErrors = true;
						}
					}
					done.Add(p.Expression);
				}
				else
					_VisitFetchTerminals(p.Expression, working);
			}

			if (hasErrors)
				return result;
			foreach (var term in working)
			{
				if (!done.Contains(term))
				{
					var newId = _GetImplicitTermId(syms);
					var found = false;
					var prod = document.GetProductionForExpression(term);
					if (null != prod)
					{
						found = true;
						// recycle this symbol
						newId = prod.Name;
					}

					if (!found)
					{
						document.Productions.Add(new XbnfProduction(newId, term));
					}
					tmap.Add(term, newId);
				}
			}
			// tmap now contains ALL of our terminal definitions from all of our imports
			// now we can use tmap and syms to help solve the rest of our productions

			var ntd = new Dictionary<string, IList<IList<string>>>();
			
			for (int ic = document.Productions.Count, i = 0; i < ic; ++i)
			{
				var p = document.Productions[i];


				if (!p.IsTerminal)
				{


					var dys = _GetDysjunctions(document, syms, tmap, attrSets, rules, p, p.Expression);
					IList<IList<string>> odys;
					if (ntd.TryGetValue(p.Name, out odys))
					{
						result.Add(new XbnfMessage(ErrorLevel.Error, -1, string.Format("The {0} production was specified more than once", p.Name), p.Line, p.Column, p.Position, document.FileOrUrl));
						hasErrors = true;
					}
					
					ntd.Add(p.Name, dys);
					if (hasErrors)
						return result;
				}
			}
			// now that we've done that, build the rest of our attributes
			foreach (var sattrs in attrSets)
			{
				CfgAttributeList attrs;
				if (!cfg.AttributeSets.TryGetValue(sattrs.Key, out attrs))
				{
					attrs = new CfgAttributeList();
					cfg.AttributeSets.Add(sattrs.Key, attrs);
				}
				for (int jc = sattrs.Value.Count, j = 0; j < jc; ++j)
				{
					var attr = sattrs.Value[j];
					attrs.Add(new CfgAttribute(attr.Name, attr.Value));
				}

			}
			
			// now write our main rules
			foreach (var nt in ntd)
			{
				foreach (var l in nt.Value)
				{
					cfg.Rules.Add(new CfgRule(nt.Key, l));

				}
			}
			// build our secondary rules
			foreach (var rule in rules)
			{
				cfg.Rules.Add(new CfgRule(rule.Key, rule.Value));

			}
			
			if (hasErrors)
				return result;
			genInfo.Xbnf = document;
			genInfo.TerminalMap = tmap;
			genInfo.Cfg = cfg;
			cfg.RebuildCache();
			return result;

		}
		static string _MergeFirstsFollows(string lhs, ICollection<string> rhs)
		{
			var result = new List<string>();
			if (null != lhs)
			{
				var sa = lhs.Trim().Split(' ');
				foreach (var s in sa)
					if (!result.Contains(s))
						result.Add(s);
			}
			foreach (var s in rhs)
			{
				if (!result.Contains(s))
					result.Add(s);
			}
			return string.Join(" ", result.ToArray());
		}
		static string _MakeSafeName(string name)
		{
			var sb = new StringBuilder();
			if (char.IsDigit(name[0]))
				sb.Append('_');
			for (var i = 0; i < name.Length; ++i)
			{
				var ch = name[i];
				if ('_' == ch || char.IsLetterOrDigit(ch))
					sb.Append(ch);
				else
					sb.Append('_');
			}
			return sb.ToString();
		}
		static string _EscapeKeyword(string name)
		{
			if (name.StartsWith("__") || _FixedStringLookup(_keywords, name))
				return "@" + name;
			return name;
		}
		static bool _FixedStringLookup(string[][] lookupTable, string value)
		{
			int length = value.Length;
			if (length <= 0 || length - 1 >= lookupTable.Length)
			{
				return false;
			}

			string[] subArray = lookupTable[length - 1];
			if (subArray == null)
			{
				return false;
			}
			return _FixedStringLookupContains(subArray, value);
		}
		// TODO: Change below to use HashSet to avoid licensing issues
		#region Lookup Tables
		// from microsoft's reference implementation of the c# code dom provider
		// This routine finds a hit within a single sorted array, with the assumption that the
		// value and all the strings are of the same length.
		private static bool _FixedStringLookupContains(string[] array, string value)
		{
			int min = 0;
			int max = array.Length;
			int pos = 0;
			char searchChar;
			while (pos < value.Length)
			{

				searchChar = value[pos];

				if ((max - min) <= 1)
				{
					// we are down to a single item, so we can stay on this row until the end.
					if (searchChar != array[min][pos])
					{
						return false;
					}
					pos++;
					continue;
				}

				// There are multiple items to search, use binary search to find one of the hits
				if (!_FindCharacter(array, searchChar, pos, ref min, ref max))
				{
					return false;
				}
				// and move to next char
				pos++;
			}
			return true;
		}

		// Do a binary search on the character array at the specific position and constrict the ranges appropriately.
		static bool _FindCharacter(string[] array, char value, int pos, ref int min, ref int max)
		{
			int index = min;
			while (min < max)
			{
				index = (min + max) / 2;
				char comp = array[index][pos];
				if (value == comp)
				{
					// We have a match. Now adjust to any adjacent matches
					int newMin = index;
					while (newMin > min && array[newMin - 1][pos] == value)
					{
						newMin--;
					}
					min = newMin;

					int newMax = index + 1;
					while (newMax < max && array[newMax][pos] == value)
					{
						newMax++;
					}
					max = newMax;
					return true;
				}
				if (value < comp)
				{
					max = index;
				}
				else
				{
					min = index + 1;
				}
			}
			return false;
		}
		static readonly string[][] _keywords = new string[][] {
			null,           // 1 character
            new string[] {  // 2 characters
                "as",
				"do",
				"if",
				"in",
				"is",
			},
			new string[] {  // 3 characters
                "for",
				"int",
				"new",
				"out",
				"ref",
				"try",
			},
			new string[] {  // 4 characters
                "base",
				"bool",
				"byte",
				"case",
				"char",
				"else",
				"enum",
				"goto",
				"lock",
				"long",
				"null",
				"this",
				"true",
				"uint",
				"void",
			},
			new string[] {  // 5 characters
                "break",
				"catch",
				"class",
				"const",
				"event",
				"false",
				"fixed",
				"float",
				"sbyte",
				"short",
				"throw",
				"ulong",
				"using",
				"while",
			},
			new string[] {  // 6 characters
                "double",
				"extern",
				"object",
				"params",
				"public",
				"return",
				"sealed",
				"sizeof",
				"static",
				"string",
				"struct",
				"switch",
				"typeof",
				"unsafe",
				"ushort",
			},
			new string[] {  // 7 characters
                "checked",
				"decimal",
				"default",
				"finally",
				"foreach",
				"private",
				"virtual",
			},
			new string[] {  // 8 characters
                "abstract",
				"continue",
				"delegate",
				"explicit",
				"implicit",
				"internal",
				"operator",
				"override",
				"readonly",
				"volatile",
			},
			new string[] {  // 9 characters
                "__arglist",
				"__makeref",
				"__reftype",
				"interface",
				"namespace",
				"protected",
				"unchecked",
			},
			new string[] {  // 10 characters
                "__refvalue",
				"stackalloc",
			},
		};
		#endregion
		static string _MakeUniqueName(ICollection<string> seen, string name)
		{
			var result = name;
			var suffix = 2;
			while (seen.Contains(result))
			{
				result = string.Concat(name, suffix.ToString());
				++suffix;
			}
			seen.Add(result);
			return result;
		}
		static string _ToRegex(XbnfDocument d, XbnfExpression e, bool first, bool gplex = false)
		{
			var le = e as XbnfLiteralExpression;
			if (null != le)
			{
				var s = _EscapeLiteral(XbnfNode.Escape(le.Value), !gplex);
				if (gplex)
				{
					s = string.Concat("\"", s, "\"");
				}
				return s;
			}
			var rxe = e as XbnfRegexExpression;
			if (null != rxe)
			{
				var r = rxe.Value;
				if (gplex)
					r = r.Replace("\"", "\\\"");
				return first ? r : string.Concat("(", r, ")");
			}
			var rfe = e as XbnfRefExpression;
			if (null != rfe)
				_ToRegex(d, d.Productions[rfe.Symbol].Expression, first, gplex);
			var re = e as XbnfRepeatExpression;
			if (null != re)
			{
				if (re.IsOptional)
					return string.Concat("(", _ToRegex(d, re.Expression, true, gplex), ")*");
				else
					return string.Concat("(", _ToRegex(d, re.Expression, true, gplex), ")+");
			}
			var oe = e as XbnfOrExpression;
			if (null != oe)
			{
				if (!first)
					return string.Concat("(", _ToRegex(d, oe.Left, false, gplex), "|", _ToRegex(d, oe.Right, false, gplex), ")");
				else
					return string.Concat(_ToRegex(d, oe.Left, false, gplex), "|", _ToRegex(d, oe.Right, false, gplex));
			}
			var oc = e as XbnfConcatExpression;
			if (null != oc)
				return string.Concat(_ToRegex(d, oc.Left, false, gplex), _ToRegex(d, oc.Right, false, gplex));
			var ope = e as XbnfOptionalExpression;
			if (null != ope)
				return string.Concat("(", _ToRegex(d, ope.Expression, true, gplex), ")?");
			return "";
		}
		static string _EscapeLiteral(string v, bool regex = true)
		{
			var sb = new StringBuilder();

			for (var i = 0; i < v.Length; ++i)
			{
				if (regex)
				{
					switch (v[i])
					{
						case '[':
						case ']':
						case '-':
						case '{':
						case '}':
						case '(':
						case ')':
						case '.':
						case '+':
						case '*':
						case '?':
						case '\'':
						case '|':
						case '<':
						case '>':
						case ';':
							//case '\\':
							sb.Append(string.Concat("\\", v[i].ToString()));
							break;
						default:
							sb.Append(v[i]);
							break;
					}
				}
				else
				{
					switch (v[i])
					{
						case '\t':
							sb.Append(@"\t");
							break;
						case '\v':
							sb.Append(@"\v");
							break;
						case '\f':
							sb.Append(@"\f");
							break;
						case '\r':
							sb.Append(@"\r");
							break;
						case '\n':
							sb.Append(@"\n");
							break;
						case '\a':
							sb.Append(@"\a");
							break;
						case '\b':
							sb.Append(@"\b");
							break;
						case '\0':
							sb.Append(@"\0");
							break;
						case '\\':
							sb.Append(@"\\");
							break;
						case '\"':
							sb.Append("\"");
							break;
						default:
							sb.Append(v[i]);
							break;
					}
				}
			}
			return sb.ToString();
		}
		static IList<IList<string>> _GetDysjunctions(
			XbnfDocument d,
			ICollection<string> syms,
			IDictionary<XbnfExpression, string> tmap,
			IDictionary<string, XbnfAttributeList> attrs,
			IList<KeyValuePair<string, IList<string>>> rules,
			XbnfProduction p,
			XbnfExpression e
			)
		{
			var le = e as XbnfLiteralExpression;
			if (null != le)
			{
				var res = new List<IList<string>>();
				var l = new List<string>();
				l.Add(tmap[le]);
				res.Add(l);
				return res;
			}
			var rxe = e as XbnfRegexExpression;
			if (null != rxe)
			{
				var res = new List<IList<string>>();
				var l = new List<string>();
				l.Add(tmap[rxe]);
				res.Add(l);
				return res;
			}
			var rfe = e as XbnfRefExpression;
			if (null != rfe)
			{
				var res = new List<IList<string>>();
				var l = new List<string>();
				l.Add(rfe.Symbol);
				res.Add(l);
				return res;
			}
			var ce = e as XbnfConcatExpression;
			if (null != ce)
				return _GetDysConcat(d, syms, tmap, attrs, rules, p, ce);

			var oe = e as XbnfOrExpression;
			if (null != oe)
				return _GetDysOr(d, syms, tmap, attrs, rules, p, oe);
			var ope = e as XbnfOptionalExpression;
			if (null != ope)
			{
				return _GetDysOptional(d, syms, tmap, attrs, rules, p, ope);
			}
			var re = e as XbnfRepeatExpression;
			if (null != re)
				return _GetDysRepeat(d, syms, tmap, attrs, rules, p, re);
			throw new NotSupportedException("The specified expression type is not supported.");
		}

		static IList<IList<string>> _GetDysOptional(XbnfDocument d, ICollection<string> syms, IDictionary<XbnfExpression, string> tmap, IDictionary<string, XbnfAttributeList> attrs, IList<KeyValuePair<string, IList<string>>> rules, XbnfProduction p, XbnfOptionalExpression ope)
		{
			var l = new List<IList<string>>();
			if (null != ope.Expression)
			{
				l.AddRange(_GetDysjunctions(d, syms, tmap, attrs, rules, p, ope.Expression));
				var ll = new List<string>();
				if (!l.Contains(ll, OrderedCollectionEqualityComparer<string>.Default))
					l.Add(ll);
			}
			return l;
		}

		static IList<IList<string>> _GetDysRepeat(XbnfDocument d, ICollection<string> syms, IDictionary<XbnfExpression, string> tmap, IDictionary<string, XbnfAttributeList> attrs, IList<KeyValuePair<string, IList<string>>> rules, XbnfProduction p, XbnfRepeatExpression re)
		{
			string sid = null;
			var sr = re.Expression as XbnfRefExpression;
			if (null != d && null != sr)
				sid = string.Concat(sr.Symbol, "List");
			if (string.IsNullOrEmpty(sid))
			{
				var cc = re.Expression as XbnfConcatExpression;
				if (null != cc)
				{
					sr = cc.Right as XbnfRefExpression;
					if (null != sr)
						sid = string.Concat(sr.Symbol, "ListTail");
				}
			}
			if (string.IsNullOrEmpty(sid))
				sid = string.Concat(p.Name, "List");
			var listId = sid;
			var i = 2;
			var ss = listId;
			while (syms.Contains(ss))
			{
				ss = string.Concat(listId, i.ToString());
				++i;
			}
			syms.Add(ss);
			listId = ss;
			var attr = new XbnfAttribute("collapsed", true);
			var attr2 = new XbnfAttribute("nowarn", true);
			var attr3 = new XbnfAttribute("factored", true);
			var attrlist = new XbnfAttributeList();
			attrlist.Add(attr);
			attrlist.Add(attr2);
			attrlist.Add(attr3);
			attrs.Add(listId, attrlist);
			var expr =
				new XbnfOrExpression(
					new XbnfConcatExpression(
						new XbnfRefExpression(listId), re.Expression), re.Expression); ;
			foreach (var nt in _GetDysjunctions(d, syms, tmap, attrs, rules, p, expr))
			{
				var l = new List<string>();
				var r = new KeyValuePair<string, IList<string>>(listId, l);
				foreach (var s in nt)
				{
					if (1 < r.Value.Count && null == s)
						continue;
					r.Value.Add(s);
				}
				rules.Add(r);
			}
			if (!re.IsOptional)
				return new List<IList<string>>(new IList<string>[] { new List<string>(new string[] { listId }) });
			else
			{
				var res = new List<IList<string>>();
				res.Add(new List<string>(new string[] { listId }));
				res.Add(new List<string>());
				return res;
			}
		}

		static IList<IList<string>> _GetDysOr(XbnfDocument d, ICollection<string> syms, IDictionary<XbnfExpression, string> tmap, IDictionary<string, XbnfAttributeList> attrs, IList<KeyValuePair<string, IList<string>>> rules, XbnfProduction p, XbnfOrExpression oe)
		{
			var l = new List<IList<string>>();
			if (null == oe.Left)
				l.Add(new List<string>());
			else
				foreach (var ll in _GetDysjunctions(d, syms, tmap, attrs, rules, p, oe.Left))
					if (!l.Contains(ll, OrderedCollectionEqualityComparer<string>.Default))
						l.Add(ll);
			if (null == oe.Right)
			{
				var ll = new List<string>();
				if (!l.Contains(ll, OrderedCollectionEqualityComparer<string>.Default))
					l.Add(ll);
			}
			else
				foreach (var ll in _GetDysjunctions(d, syms, tmap, attrs, rules, p, oe.Right))
					if (!l.Contains(ll, OrderedCollectionEqualityComparer<string>.Default))
						l.Add(ll);
			return l;
		}

		static IList<IList<string>> _GetDysConcat(XbnfDocument d, ICollection<string> syms, IDictionary<XbnfExpression, string> tmap, IDictionary<string, XbnfAttributeList> attrs, IList<KeyValuePair<string, IList<string>>> rules, XbnfProduction p, XbnfConcatExpression ce)
		{
			var l = new List<IList<string>>();
			if (null == ce.Right)
			{
				if (null == ce.Left) return l;
				foreach (var ll in _GetDysjunctions(d, syms, tmap, attrs, rules, p, ce.Left))
					l.Add(new List<string>(ll));
				return l;
			}
			else if (null == ce.Left)
			{
				foreach (var ll in _GetDysjunctions(d, syms, tmap, attrs, rules, p, ce.Right))
					l.Add(new List<string>(ll));
				return l;
			}
			foreach (var ll in _GetDysjunctions(d, syms, tmap, attrs, rules, p, ce.Left))
			{
				foreach (var ll2 in _GetDysjunctions(d, syms, tmap, attrs, rules, p, ce.Right))
				{
					var ll3 = new List<string>();
					ll3.AddRange(ll);
					ll3.AddRange(ll2);
					if (!l.Contains(ll3, OrderedCollectionEqualityComparer<string>.Default))
						l.Add(ll3);
				}
			}
			return l;
		}
		static string _GetImplicitTermId(ICollection<string> syms)
		{
			var result = "Implicit";
			var i = 2;
			while (syms.Contains(result))
			{
				result = string.Concat("Implicit", i.ToString());
				++i;
			}
			syms.Add(result);
			return result;
		}
		static void _VisitFetchTerminals(XbnfExpression expr, HashSet<XbnfExpression> terms)
		{
			var l = expr as XbnfLiteralExpression;
			if (null != l)
			{
				if (!terms.Contains(l))
					terms.Add(l);
				return;
			}
			var r = expr as XbnfRegexExpression;
			if (null != r)
			{
				if (!terms.Contains(r))
					terms.Add(r);
				return;
			}
			var u = expr as XbnfUnaryExpression;
			if (null != u)
			{
				_VisitFetchTerminals(u.Expression, terms);
				return;
			}
			var b = expr as XbnfBinaryExpression;
			if (null != b)
			{
				_VisitFetchTerminals(b.Left, terms);
				_VisitFetchTerminals(b.Right, terms);
				return;
			}

		}
		static void _VisitUnreferenced(XbnfDocument doc, XbnfExpression expr, HashSet<string> result)
		{
			var r = expr as XbnfRefExpression;
			if (null != r)
			{
				if (!doc.Productions.Contains(r.Symbol))
					result.Add(r.Symbol);
				return;
			}
			var u = expr as XbnfUnaryExpression;
			if (null != u)
			{
				_VisitUnreferenced(doc, u.Expression, result);
				return;
			}
			var b = expr as XbnfBinaryExpression;
			if (null != b)
			{
				_VisitUnreferenced(doc, b.Left, result);
				_VisitUnreferenced(doc, b.Right, result);
				return;
			}

		}
	}
}
