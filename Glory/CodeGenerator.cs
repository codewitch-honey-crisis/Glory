using CD;
using Slang;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace Glory
{
	using C = CodeDomUtility;
	using V = CodeDomVisitor;
	static class CodeGenerator
	{
		public static readonly CodeAttributeDeclaration GeneratedCodeAttribute
			= new CodeAttributeDeclaration(C.Type(typeof(GeneratedCodeAttribute)), new CodeAttributeArgument(C.Literal(Program.Name)), new CodeAttributeArgument(C.Literal(Assembly.GetExecutingAssembly().GetName().Version.ToString())));
		public static CodeCompileUnit GenerateCompileUnit(XbnfGenerationInfo genInfo, string codeclass, string codenamespace, bool fast)
		{
			var result = new CodeCompileUnit();
			var ns = new CodeNamespace();
			if (!string.IsNullOrEmpty(codenamespace))
				ns.Name = codenamespace;
			result.Namespaces.Add(ns);
			ns.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
			var td = C.Class(codeclass, false);
			td.CustomAttributes.Add(GeneratedCodeAttribute);
			td.BaseTypes.Add(C.Type("GlrTableParser"));
			// GlrTableParser(int[][][][] parseTable,string[] symbolTable, ParseAttribute[][] attributes,int[] errorSentinels, IEnumerable<Token> tokenizer,int maxErrorCount)
			td.Members.Add(C.Field(C.Type(typeof(int[][][][])), "ParseTable", MemberAttributes.FamilyAndAssembly | MemberAttributes.Static));
			td.Members.Add(C.Field(C.Type(typeof(string[])), "SymbolTable", MemberAttributes.FamilyAndAssembly | MemberAttributes.Static));
			td.Members.Add(C.Field(C.Type(C.Type("ParseAttribute", 1), 1), "ParseAttributes", MemberAttributes.FamilyAndAssembly | MemberAttributes.Static));
			td.Members.Add(C.Field(C.Type(typeof(int[])), "ErrorSentinels", MemberAttributes.FamilyAndAssembly | MemberAttributes.Static));
			//td.Members.Add(C.Field(C.Type(typeof(int[])), "NodeFlags", MemberAttributes.FamilyAndAssembly | MemberAttributes.Static));
			
			var et = C.Type("IEnumerable");
			et.TypeArguments.Add(C.Type("Token"));
			var ctor = C.Ctor(MemberAttributes.Public, C.Param(et, "tokenizer"));
			ctor.BaseConstructorArgs.AddRange(new CodeExpression[] {
				C.FieldRef(C.TypeRef(codeclass), "ParseTable"),
				C.FieldRef(C.TypeRef(codeclass), "SymbolTable"),
				C.FieldRef(C.TypeRef(codeclass), "ParseAttributes"),
				C.FieldRef(C.TypeRef(codeclass), "ErrorSentinels"),
				//C.FieldRef(C.TypeRef(codeclass), "NodeFlags"),
				C.ArgRef("tokenizer"),
				C.FieldRef(C.TypeRef(typeof(int)), "MaxValue") });
			td.Members.Add(ctor);
			ctor = C.Ctor(MemberAttributes.Public, C.Param(et, "tokenizer"),C.Param(typeof(int),"maxErrorCount"));
			ctor.BaseConstructorArgs.AddRange(new CodeExpression[] {
				C.FieldRef(C.TypeRef(codeclass), "ParseTable"),
				C.FieldRef(C.TypeRef(codeclass), "SymbolTable"),
				C.FieldRef(C.TypeRef(codeclass), "ParseAttributes"),
				C.FieldRef(C.TypeRef(codeclass), "ErrorSentinels"),
				//C.FieldRef(C.TypeRef(codeclass), "NodeFlags"),
				C.ArgRef("tokenizer"),
				C.ArgRef("maxErrorCount")});
			td.Members.Add(ctor);
			ns.Types.Add(td);
			CfgGlrParseTable pt;
			genInfo.Cfg.TryToGlrParseTable(out pt);
			int ts;
			var syms = XbnfConvert.GetSymbolTable(genInfo, out ts);

			(C.GetByName("ParseTable", td.Members) as CodeMemberField).InitExpression = C.Literal(pt.ToArray(syms));
			(C.GetByName("SymbolTable", td.Members) as CodeMemberField).InitExpression = C.Literal(syms.ToArray());
			(C.GetByName("ParseAttributes", td.Members) as CodeMemberField).InitExpression = _SerializeParseAttributes(genInfo,syms);
			(C.GetByName("ErrorSentinels", td.Members) as CodeMemberField).InitExpression = _SerializeErrorSentinels(genInfo,syms);
			//(C.GetByName("NodeFlags", td.Members) as CodeMemberField).InitExpression = _SerializeNodeFlags(genInfo,syms);
			foreach(var code in genInfo.Xbnf.Code)
				td.Members.AddRange(SlangParser.ParseMembers(code.Value,code.Line,code.Column,code.Position));
			var hasChangeType = false;
			var hasEvalAny = false;
			foreach(var prod in genInfo.Xbnf.Productions)
			{
				if(null!=prod.Action)
				{
					var hasEA = false;
					var hasCT = false;
					_GenerateAction(genInfo,syms,td,prod,fast,out hasCT,out hasEA);
					if (hasCT)
						hasChangeType = true;
					if (hasEA)
						hasEvalAny = true;
				}
			}
			var consts = new string[syms.Count];
			for (int ic = syms.Count, i = 0; i < ic; ++i)
			{
				var s = syms[i];
				if ("#ERROR" == s)
					s = "ErrorSymbol";
				else if ("#EOS" == s)
					s = "EosSymbol";
				s = _MakeSafeName(s);
				s = _MakeUniqueMember(td, s);
				consts[i] = s;
				td.Members.Add(C.Field(typeof(int), s, MemberAttributes.Const | MemberAttributes.Public, C.Literal(i)));
			}
			if (hasChangeType)
			{
				var m = C.Method(C.Type(typeof(object)), "_ChangeType", MemberAttributes.Static | MemberAttributes.Private, C.Param(typeof(object), "obj"), C.Param(typeof(Type), "type"));
				m.Statements.Add(C.Var(typeof(TypeConverter), "typeConverter", C.Invoke(C.TypeRef(typeof(TypeDescriptor)), "GetConverter", C.ArgRef("obj"))));
				// if(null!=typeConverter || !typeConverter.CanConvertTo(type))
				m.Statements.Add(C.If(C.Or(C.IdentEq(C.Null, C.VarRef("typeConverter")), C.Not(C.Invoke(C.VarRef("typeConverter"), "CanConvertTo", C.ArgRef("type")))),
					C.Return(C.Invoke(C.TypeRef(typeof(Convert)), "ChangeType", C.ArgRef("obj"), C.ArgRef("type")))
					));
				m.Statements.Add(C.Return(C.Invoke(C.VarRef("typeConverter"), "ConvertTo", C.ArgRef("obj"), C.ArgRef("type"))));
				td.Members.Add(m);
			}
			if (hasEvalAny)
			{
				var sid = C.PropRef(C.ArgRef("node"), "SymbolId");
				var m = C.Method(typeof(object), "_EvaluateAny", MemberAttributes.Private | MemberAttributes.Static, C.Param(C.Type("ParseNode"), "node"), C.Param(typeof(object), "state"));
				for (int ic = genInfo.Xbnf.Productions.Count, i = 0; i < ic; ++i)
				{
					var p = genInfo.Xbnf.Productions[i];
					if (!p.IsCollapsed && !p.IsHidden)
					{

						var sidcmp = syms.IndexOf(p.Name);
						var sidcf = C.FieldRef(C.TypeRef(td.Name), consts[sidcmp]);
						var cnd = C.If(C.Eq(sid, sidcf));
						if (!p.IsTerminal)
							cnd.TrueStatements.Add(C.Return(C.Invoke(C.TypeRef(td.Name), string.Concat("Evaluate", p.Name), C.ArgRef("node"), C.ArgRef("state"))));
						else
							cnd.TrueStatements.Add(C.Return(C.PropRef(C.ArgRef("node"), "Value")));
						m.Statements.Add(cnd);
					}
				}
				m.Statements.Add(C.Return(C.Null));
				td.Members.Add(m);
			}
			
			return result;
		}
		static void _GenerateAction(XbnfGenerationInfo info,IList<string> syms,CodeTypeDeclaration parser,XbnfProduction prod,bool fast,out bool hasChangeType,out bool hasEvalAny)
		{
			var isStart = ReferenceEquals(prod, info.Xbnf.StartProduction);
			var isShared = false;
			hasChangeType = false;
			hasEvalAny = false;
			var hasReturn = false;
			var ai = prod.Attributes.IndexOf("shared");
			if(-1<ai)
			{
				var o = prod.Attributes[ai].Value;
				if (o is bool && (bool)o)
					isShared = true;
			}
			var type = new CodeTypeReference(typeof(object));
			ai = prod.Attributes.IndexOf("type");
			if (-1 < ai)
			{
				var s = prod.Attributes[ai].Value as string;
				if (!string.IsNullOrEmpty(s))
				{
					//type = new CodeTypeReference(CodeDomResolver.TranslateIntrinsicType(s));
					type = SlangParser.ParseType(s);
				}
			}
			MemberAttributes pattrs = MemberAttributes.Public | MemberAttributes.Static;
			MemberAttributes attrs = MemberAttributes.FamilyAndAssembly | MemberAttributes.Static;
			var rs = new StringBuilder();
			foreach (var r in info.Cfg.FillNonTerminalRules(prod.Name))
				rs.AppendLine(r.ToString());

			if (isStart)
			{
				var ms = C.Method(type, "Evaluate", pattrs, C.Param(C.Type("ParseNode"), "node"));
				ms.Comments.AddRange(C.ToComments(string.Format("<summary>\r\nEvaluates a derivation of the form:\r\n{0}\r\n</summary>\r\n<remarks>\r\nThe production rules are:\r\n{1}\r\n</remarks>\r\n<param name=\"node\">The <see cref=\"ParseNode\"/> to evaluate</param>\r\n<returns>The result of the evaluation</returns>", prod.ToString("p").TrimEnd(), rs.ToString().TrimEnd()), true));
				ms.Statements.Add(C.Return(C.Invoke(C.TypeRef(parser.Name), string.Concat("Evaluate", prod.Name), C.ArgRef("node"),C.Null)));
				parser.Members.Add(ms);
				ms = C.Method(type, "Evaluate", pattrs, C.Param(C.Type("ParseNode"), "node"), C.Param(C.Type(typeof(object)), "state"));
				ms.Comments.AddRange(C.ToComments(string.Format("<summary>\r\nEvaluates a derivation of the form:\r\n{0}\r\n</summary>\r\n<remarks>\r\nThe production rules are:\r\n{1}\r\n</remarks>\r\n<param name=\"node\">The <see cref=\"ParseNode\"/> to evaluate</param>\r\n<param name=\"state\">A user supplied state object. What it should be depends on the production's associated code block</param>\r\n<returns>The result of the evaluation</returns>", prod.ToString("p").TrimEnd(), rs.ToString().TrimEnd()), true));
				ms.Statements.Add(C.Return(C.Invoke(C.TypeRef(parser.Name), string.Concat("Evaluate", prod.Name), C.ArgRef("node"), C.ArgRef("state"))));
				parser.Members.Add(ms);
			}
			var m = C.Method(type, string.Concat("Evaluate", prod.Name), (!isStart && isShared)?pattrs:attrs, C.Param("ParseNode", "node"), C.Param(typeof(object), "state"));
			m.Comments.AddRange(C.ToComments(string.Format("<summary>\r\nEvaluates a derivation of the form:\r\n{0}\r\n</summary>\r\n<remarks>\r\nThe production rules are:\r\n{1}\r\n</remarks>\r\n<param name=\"node\">The <see cref=\"ParseNode\"/> to evaluate</param>\r\n<param name=\"state\">A user supplied state object. What it should be depends on the production's associated code block</param>\r\n<returns>The result of the evaluation</returns>", prod.ToString("p").TrimEnd(), rs.ToString().TrimEnd()), true));
			var stmts = SlangParser.ParseStatements(prod.Action.Value, prod.Action.Line,prod.Action.Column,prod.Action.Position);
			bool hasCT, hasE, hasR;
		
			_TranslateMacros(info, parser, stmts, type, out hasCT, out hasE, out hasR);
			if (hasCT)
				hasChangeType = true;
			if (hasE)
				hasEvalAny = true;
			if (hasR)
				hasReturn = true;
			if (!hasReturn)
			{
				if (!CD.CodeDomResolver.IsNullOrVoidType(type))
					stmts.Add(C.Return(C.Default(type)));
				else
					stmts.Add(C.Return(C.Null));
			}
			
			m.Statements.AddRange(stmts);
			parser.Members.Add(m);
		}
		static void _TranslateMacros(XbnfGenerationInfo info, CodeTypeDeclaration parser,CodeStatementCollection stmts,CodeTypeReference type, out bool hasChangeType,out bool hasEvalAny, out bool hasReturn)
		{
			hasEvalAny = false;
			hasChangeType = false;
			hasReturn = false;
			var hasEA = false;
			var hasCT = false;
			var hasR = false;
			var node = C.ArgRef("node");
			for(int ic=stmts.Count, i = 0;i<ic;++i)
			{
				var stmt = stmts[i];
				V.Visit(stmt, (ctx) => {
					var idx = ctx.Target as CodeIndexerExpression;
					if (null != idx && 1 == idx.Indices.Count)
					{
						var to = idx.TargetObject as CodeVariableReferenceExpression;
						if (null != to)
						{
							int pi;
							if (0 == string.Compare("Child", to.VariableName, StringComparison.InvariantCulture))
							{
								// is a thing like Child[0]
								hasEA = true;
								var mi = C.Invoke(C.TypeRef(parser.Name), "_EvaluateAny", C.ArrIndexer(C.PropRef(node, "Children"), idx.Indices[0]), C.ArgRef("state"));
								V.ReplaceTarget(ctx, mi);
							}
							else if (-1 < (pi = info.Xbnf.Productions.IndexOf(to.VariableName)))
							{
								// is a thing like Factor[0]
								var p = info.Xbnf.Productions[pi];
								if (!p.IsCollapsed && !p.IsHidden)
								{
									if (!p.IsTerminal)
									{
										var mi = C.Invoke(C.TypeRef(parser.Name), string.Concat("Evaluate", p.Name), C.ArrIndexer(C.PropRef(node, "Children"), idx.Indices[0]), C.ArgRef("state"));
										V.ReplaceTarget(ctx, mi);
									}
									else
									{
										var pr = C.PropRef(C.ArrIndexer(C.PropRef(node, "Children"), idx.Indices[0]), "Value");
										V.ReplaceTarget(ctx, pr);
									}
								}
							}
							else if (0 == string.Compare("SymbolId", to.VariableName))
							{
								var pr = C.PropRef(C.ArrIndexer(C.PropRef(node, "Children"), idx.Indices[0]), "SymbolId");
								V.ReplaceTarget(ctx, pr);
							}
						}
					}
					var v = ctx.Target as CodeVariableReferenceExpression;
					if (null != v)
					{
						foreach (var p in info.Xbnf.Productions)
						{
							if (p.IsHidden || p.IsCollapsed)
								continue;
							if (v.VariableName.StartsWith(p.Name, StringComparison.InvariantCulture))
							{
								if (p.Name.Length < v.VariableName.Length)
								{
									var s = v.VariableName.Substring(p.Name.Length);
									int num;
									if (int.TryParse(s, out num))
					 				{
										if (0 < num)
										{
											if (!p.IsTerminal)
											{
												var mi = C.Invoke(C.TypeRef(parser.Name), string.Concat("Evaluate", p.Name), C.ArrIndexer(C.PropRef(node, "Children"), C.Literal(num - 1)), C.ArgRef("state"));
												V.ReplaceTarget(ctx, mi);
											}
											else
											{
												var pr = C.PropRef(C.ArrIndexer(C.PropRef(node, "Children"), C.Literal(num - 1)), "Value");
												V.ReplaceTarget(ctx, pr);
											}
										}
									}
								}
							}
							else if (v.VariableName.StartsWith("Child", StringComparison.InvariantCulture))
							{
								if (5 < v.VariableName.Length)
								{
									var s = v.VariableName.Substring(5);
									int num;
									if (int.TryParse(s, out num))
									{
										if (0 < num)
										{
											hasEA = true;
											var mi = C.Invoke(C.TypeRef(parser.Name), "_EvaluateAny", C.ArrIndexer(C.PropRef(node, "Children"), C.Literal(num - 1)), C.ArgRef("state"));
											V.ReplaceTarget(ctx, mi);

										}
									}
								}
							}
							else if (0 == string.Compare("Length", v.VariableName, StringComparison.InvariantCulture))
							{
								var ffr = C.PropRef(C.PropRef(node, "Children"), "Length");
								V.ReplaceTarget(ctx, ffr);
							}
							else
							{
								if (v.VariableName.StartsWith("SymbolId", StringComparison.InvariantCulture))
								{
									if (8 < v.VariableName.Length)
									{
										var s = v.VariableName.Substring(8);
										int num;
										if (int.TryParse(s, out num))
										{
											if (0 < num)
											{

												var pr = C.PropRef(C.ArrIndexer(C.PropRef(node, "Children"), C.Literal(num - 1)), "SymbolId");
												V.ReplaceTarget(ctx, pr);

											}
										}
									}
								}
							}
						}
					}
				/*});
				V.Visit(stmt, (ctx) =>
				{*/
					var r = ctx.Target as CodeMethodReturnStatement;
					if (null != r)
					{
						if (!CD.CodeDomResolver.IsNullOrVoidType(type) && (0 != type.ArrayRank || 0 != string.Compare("System.Object", type.BaseType, StringComparison.InvariantCulture)))
						{
							var hasVoid = false;
							if (null != r.Expression)
							{
								var p = r.Expression as CodePrimitiveExpression;
								if (null != p)
								{
									if (null == p.Value)
										hasVoid = true;
								}
							}
							if (null == r.Expression || hasVoid)
							{
								r.Expression = C.Default(type);
							}
							else
							{
								var isType = false;
								var cc = r.Expression as CodeCastExpression;
								if (null != cc)
								{
									if (CD.CodeTypeReferenceEqualityComparer.Equals(cc.TargetType, type))
										isType = true;
								}
								if (!isType)
								{
									hasCT = true;
									r.Expression = C.Cast(type, C.Invoke(C.TypeRef(parser.Name), "_ChangeType", r.Expression, C.TypeOf(type)));
								}
							}
						}
						hasR = true;
					}
				});
				if (hasR)
					hasReturn = true;
				
			}
			hasChangeType = hasCT;
			hasEvalAny = hasEA;
		}
		static CodeExpression _SerializeErrorSentinels(XbnfGenerationInfo info,List<string> syms)
		{
			var result = new List<int>();
			foreach(var attrs in info.Cfg.AttributeSets)
			{
				var ai = attrs.Value.IndexOf("errorSentinel");
				if(-1<ai)
				{
					var o = attrs.Value[ai].Value;
					if(o is bool && (bool)o)
					{
						result.Add(syms.IndexOf(attrs.Key));
					}
				}
			}
			return C.Literal(result.ToArray());
		}
		/*static CodeExpression _SerializeNodeFlags(XbnfGenerationInfo info, List<string> syms)
		{
			var result = new int[syms.Count];
			foreach (var attrs in info.Cfg.AttributeSets)
			{
				var ai = attrs.Value.IndexOf("collapsed");
				if (-1 < ai)
				{
					var o = attrs.Value[ai].Value;
					if (o is bool && (bool)o)
					{
						result[syms.IndexOf(attrs.Key)]=1;
					}
				}
			}
			return C.Literal(result);
		}*/
		static CodeExpression _SerializeParseAttributes(XbnfGenerationInfo info,List<string> syms)
		{
			var arr = new ParseAttribute[syms.Count][];
			foreach(var aset in info.Cfg.AttributeSets)
			{
				var id = syms.IndexOf(aset.Key);
				int ic = aset.Value.Count;
				arr[id] = new ParseAttribute[ic];
				for(var i =0;i<ic;++i)
				{
					var a = aset.Value[i];
					arr[id][i] = new ParseAttribute(a.Name, a.Value);
				}
			}
			for(var i = 0;i<arr.Length;i++)
			{
				if (null == arr[i])
					arr[i] = new ParseAttribute[0];
			}
			return _SerializeParseAttributes(arr);
		}
		static CodeExpression _SerializeParseAttributes(ParseAttribute[][] attrs)
		{
			var result = C.Literal(attrs);
			V.Visit(result, (ctx) => {
				var t = ctx.Target as CodeTypeReference;
				if(null!=t)
				{
					if (t.BaseType.EndsWith(".ParseAttribute"))
						t.BaseType = "ParseAttribute";
				}
			});
			return result;
		}
		public static CodeCompileUnit GenerateSharedCompileUnit(string codenamespace)
		{
			var result = new CodeCompileUnit();
			var ns = new CodeNamespace();
			if (!string.IsNullOrEmpty(codenamespace))
				ns.Name = codenamespace;
			result.Namespaces.Add(ns);
			ImportCompileUnit(ns, Deslanged.GlrTableParser);
			ImportCompileUnit(ns, Deslanged.GlrWorker);
			ImportCompileUnit(ns, Deslanged.LookAheadEnumerator);
			ImportCompileUnit(ns, Deslanged.LRNodeType);
			ImportCompileUnit(ns, Deslanged.ParseAttribute);
			ImportCompileUnit(ns, Deslanged.ParseNode);
			ImportCompileUnit(ns, Deslanged.Token);
			V.Visit(result, (ctx) => {
				var ctr = ctx.Target as CodeTypeReference;
				if (null != ctr)
				{
					if (ctr.BaseType.StartsWith("Glory."))
						ctr.BaseType = ctr.BaseType.Substring(6);
				}
			});
			return result;
		}
		public static void ImportCompileUnit(CodeNamespace cns,CodeCompileUnit src)
		{
			_AddNSImports(cns, src);
			foreach(CodeNamespace ns in src.Namespaces)
			{
				foreach(CodeTypeDeclaration td in ns.Types)
				{
					var found = false;
					foreach(CodeAttributeDeclaration a in td.CustomAttributes)
					{
						if(a.AttributeType.BaseType.Equals("System.CodeDom.Compiler.GeneratedCodeAttribute"))
						{
							found = true;
							break;
						}
					}
					if(!found)
						td.CustomAttributes.Add(GeneratedCodeAttribute);
					cns.Types.Add(td);
				}
			}
		}
		static void _AddNSImports(CodeNamespace cns, CodeCompileUnit src)
		{
			foreach(CodeNamespace ns in src.Namespaces)
			{
				foreach(CodeNamespaceImport nsi in ns.Imports)
				{
					var n = nsi.Namespace;
					var found = false;
					foreach(CodeNamespaceImport nsic in cns.Imports)
					{
						if(nsic.Namespace==n)
						{
							found = true;
							break;
						}
					}
					if (!found)
						cns.Imports.Add(new CodeNamespaceImport(n));
				}
			}
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
		static string _MakeUniqueMember(CodeTypeDeclaration decl, string name)
		{
			var seen = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
			for (int ic = decl.Members.Count, i = 0; i < ic; i++)
				seen.Add(decl.Members[i].Name);
			var result = name;
			var suffix = 2;
			while (seen.Contains(result))
			{
				result = string.Concat(name, suffix.ToString());
				++suffix;
			}
			return result;
		}
	}
}
