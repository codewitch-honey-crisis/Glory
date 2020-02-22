using System;
using System.Collections.Generic;
using System.Text;

namespace Glory
{
	/// <summary>
	/// Represents a CFG rule
	/// </summary>
#if CFGLIB
	public
#endif
		class CfgRule : CfgNode, IEquatable<CfgRule>, ICloneable
	{
		/// <summary>
		/// Constructs a new rule from the specified parameters
		/// </summary>
		/// <param name="left">The left hand side of the rule</param>
		/// <param name="right">The right hand side of the rule</param>
		public CfgRule(string left,params string[] right) :
			this(left,(IEnumerable<string>)right)
		{
			
		}
		/// <summary>
		/// Constructs a new rule from the specified parameters
		/// </summary>
		/// <param name="left">The left hand side of the rule</param>
		/// <param name="right">The right hand side of the rule</param>
		public CfgRule(string left, IEnumerable<string> right)
		{
			Left = left;
			foreach(var s in right)
				Right.Add(s);
		}
		/// <summary>
		/// Indicates the left hand side of the rule
		/// </summary>
		public string Left { get; set; }
		/// <summary>
		/// Indicates the right hand side of the rule
		/// </summary>
		public IList<string> Right { get; } = new List<string>();
		/// <summary>
		/// Indicates whether the rule is an epsilon rule of the form A ->
		/// </summary>
		public bool IsNil { get { return 0 == Right.Count; } }
		/// <summary>
		/// Indicates whether any of the right hand of the rule refers to the left hand of the rule
		/// </summary>
		public bool IsDirectlyRecursive {
			get {
				for (int ic = Right.Count, i = 0; i < ic; ++i)
				{
					if (Right[i] == Left)
						return true;
				}
				return false;
			}
		}
		/// <summary>
		/// Indicates whether the first symbol on the right hand side of the rule refers to the left hand of the rule.
		/// </summary>
		public bool IsDirectlyLeftRecursive { get { return !IsNil && Right[0] == Left; } }
		/// <summary>
		/// Provides a string representation of the rule
		/// </summary>
		/// <param name="fmt">The format specifier. Can be null or "y"</param>
		public string ToString(string fmt)
		{
			var sb = new StringBuilder();
			sb.Append(Left);
			if ("y" == fmt)
				sb.Append(" :");
			else
				sb.Append(" ->");
			for(int ic=Right.Count,i=0;i<ic;++i)
			{
				sb.Append(" ");
				sb.Append(Right[i]);
			}
			if ("y" == fmt)
				sb.Append(";");
			return sb.ToString();
		}
		/// <summary>
		/// Provides a string representation of the rule
		/// </summary>
		/// <returns>Returns a rule of the form A -> a A b suitable for use with many grammar tools such as http://hackingoff.com/compilers/ll-1-parser-generator</returns>
		public override string ToString()
		{
			return ToString(null);
		}
		/// <summary>
		/// Returns a deep copy of the rule
		/// </summary>
		/// <returns>A new rule that is equivelent to the given rule</returns>
		public CfgRule Clone()
		{
			var result = new CfgRule(Left, Right);
			result.SourceElement = this;
			return result;
		}
		object ICloneable.Clone()
			=> Clone();

		#region Value semantics
		public bool Equals(CfgRule rhs)
		{
			if (ReferenceEquals(rhs, this)) return true;
			if (ReferenceEquals(rhs, null)) return false;
			if (Left != rhs.Left) return false;
			if (Right.Count != rhs.Right.Count) return false;
			for(int ic = Right.Count, i = 0; i < ic; ++i)
			{
				if (!Right[i].Equals(rhs.Right[i], StringComparison.Ordinal))
					return false;
			}
			return true;
		}
		public override bool Equals(object rhs)
			=> Equals(rhs as CfgRule);

		public override int GetHashCode()
		{
			var result = 0;
			if (null != Left)
				result ^= Left.GetHashCode();
			for(int ic=Right.Count,i=0;i<ic;++i)
				result ^= Right[i].GetHashCode();
			return result;
		}
		public static bool operator ==(CfgRule lhs, CfgRule rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return true;
			if (ReferenceEquals(lhs, null)) return false;
			return lhs.Equals(rhs);
		}
		public static bool operator !=(CfgRule lhs, CfgRule rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return false;
			if (ReferenceEquals(lhs, null)) return true;
			return !lhs.Equals(rhs);
		}
		#endregion

	}
}
