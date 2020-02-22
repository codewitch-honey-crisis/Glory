using LC;
using System;
using System.Text;

namespace Glory
{
	/// <summary>
	/// Indicates an attribute in a CFG document
	/// </summary>
	/// <remarks>Attributes are not part of CFG canon. They are markup metadata used to apply extra operations, such as collapsing a parse node in the grammar or hiding a terminal. One attribute list is associated with each symbol.</remarks>
#if CFGLIB
	public
#endif
		class CfgAttribute : CfgNode, IEquatable<CfgAttribute>, ICloneable
	{
		/// <summary>
		/// Constructs a new attribute with the specified name and value
		/// </summary>
		/// <param name="name">The name of the attribute</param>
		/// <param name="value">The value of the attribute</param>
		public CfgAttribute(string name, object value)
		{
			Name = name;
			Value = value;
		}
		/// <summary>
		/// Constructs an empty instance of an attribute
		/// </summary>
		public CfgAttribute() { }
		/// <summary>
		/// Indicates the name of the attribute
		/// </summary>
		public string Name { get; set; } = null;
		/// <summary>
		/// Indicates the value of the attribute
		/// </summary>
		public object Value { get; set; } = null;
		/// <summary>
		/// Provides a string representation of the attribute
		/// </summary>
		/// <returns>A string that represents the attribute</returns>
		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.Append(Name);
			if (!(Value is bool) || !(bool)Value)
			{
				sb.Append("= ");
				var s = Value as string;
				if (null != s)
				{
					sb.Append("\"");
					for (var i = 0; i < s.Length; i++)
						_EscAttrValChar(s[i], sb);
					sb.Append("\"");
				}
				else
					sb.Append(Value);
			}
			return sb.ToString();
		}
		/// <summary>
		/// Creates a deep copy of the attribute
		/// </summary>
		/// <returns>A new attribute that is equivelent to this attribute</returns>
		public CfgAttribute Clone()
		{
			var result = new CfgAttribute(Name, Value);
			result.SourceElement = this;
			return result;
		}
		object ICloneable.Clone()
			=> Clone();
		static void _EscAttrValChar(char ch, StringBuilder builder)
		{
			switch (ch)
			{
				case '\\':
					builder.Append('\\');
					builder.Append(ch);
					return;
				case '\t':
					builder.Append("\\t");
					return;
				case '\n':
					builder.Append("\\n");
					return;
				case '\r':
					builder.Append("\\r");
					return;
				case '\0':
					builder.Append("\\0");
					return;
				case '\f':
					builder.Append("\\f");
					return;
				case '\v':
					builder.Append("\\v");
					return;
				case '\b':
					builder.Append("\\b");
					return;
				default:
					if (!char.IsLetterOrDigit(ch) && !char.IsSeparator(ch) && !char.IsPunctuation(ch) && !char.IsSymbol(ch))
					{

						builder.Append("\\u");
						builder.Append(unchecked((ushort)ch).ToString("x4"));

					}
					else
						builder.Append(ch);
					break;
			}
			
		}
		static string _ParseAttrName(LexContext pc)
		{
			var l = pc.CaptureBuffer.Length;
			pc.TryReadUntil(false, '(', ')', '[', ']', '{', '}', '<', '>', ',', ':', ';', '=', '|', '/', '\'', '\"', ' ', '\t', '\r', '\n', '\f', '\v');
			return pc.GetCapture(l);
		}
		/// <summary>
		/// Parses an attribute from a LexContext
		/// </summary>
		/// <param name="pc">The LexContext</param>
		/// <returns>A new attribute parsed from the specified input</returns>
		internal static CfgAttribute Parse(LexContext pc)
		{
			SkipCommentsAndWhitespace(pc);
			var attr = new CfgAttribute();
			attr.SetLocation(pc.Line, pc.Column, pc.Position,pc.FileOrUrl);
			attr.Name = _ParseAttrName(pc);
			SkipCommentsAndWhitespace(pc);
			pc.Expecting(',', '=', ',', '>','\n');
			if ('=' == pc.Current)
			{
				pc.Advance();
				attr.Value = pc.ParseJsonValue();
			}
			else
				attr.Value = true;
			return attr;
		}

		#region Value semantics
		public bool Equals(CfgAttribute rhs)
		{
			if (ReferenceEquals(rhs, this)) return true;
			if (ReferenceEquals(rhs, null)) return false;
			return Name == rhs.Name && Equals(Value, rhs.Value);
		}
		public override bool Equals(object rhs)
			=> Equals(rhs as CfgAttribute);

		public override int GetHashCode()
		{
			if (null != Value)
				return Value.GetHashCode();
			return 0;
		}
		public static bool operator ==(CfgAttribute lhs, CfgAttribute rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return true;
			if (ReferenceEquals(lhs, null)) return false;
			return lhs.Equals(rhs);
		}
		public static bool operator !=(CfgAttribute lhs, CfgAttribute rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return false;
			if (ReferenceEquals(lhs, null)) return true;
			return !lhs.Equals(rhs);
		}
		#endregion

	}
}
