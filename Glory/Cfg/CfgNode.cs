using LC;

namespace Glory
{
	/// <summary>
	/// Provides core services for a node in a grammar
	/// </summary>
#if CFGLIB
	public
#endif
		abstract class CfgNode
	{
		/// <summary>
		/// Indicates the 1 based line of the node
		/// </summary>
		public int Line { get; set; } = 1;
		/// <summary>
		/// Indicates the 1 based column of the node
		/// </summary>
		public int Column { get; set; } = 1;
		/// <summary>
		/// Indicates the 0 based position of the node
		/// </summary>
		public long Position { get; set; } = 0L;
		/// <summary>
		/// Indicates the file or URL where the node is sourced, if any
		/// </summary>
		public string FileOrUrl { get; set; }
		/// <summary>
		/// Indicates the source element that produced this node. This will be filled if the CFG is generated from a high level specification file
		/// </summary>
		public object SourceElement { get; set; }
		/// <summary>
		/// Sets the location info for a node
		/// </summary>
		/// <param name="line">The 1 based line</param>
		/// <param name="column">The 1 based column</param>
		/// <param name="position">The 0 based position</param>
		/// <param name="fileOrUrl">The file or URL</param>
		public void SetLocation(int line, int column, long position,string fileOrUrl)
		{
			Line = line;
			Column = column;
			Position = position;
			FileOrUrl = fileOrUrl;
		}
		internal static string ParseIdentifier(LexContext pc)
		{
			var l = pc.CaptureBuffer.Length;
			pc.TryReadUntil(false, '(', ')', '[', ']', '{', '}', '<', '>', ',', ':', ';','-', '=', '|', '/', '\'', '\"', ' ', '\t', '\r', '\n', '\f', '\v');
			return pc.GetCapture(l);
		}
		static bool _SkipWhiteSpace(LexContext pc)
		{
			pc.EnsureStarted();
			if (-1 == pc.Current || '\n'==pc.Current || !char.IsWhiteSpace((char)pc.Current))
				return false;
			while (-1 != pc.Advance() && '\n'!=pc.Current && char.IsWhiteSpace((char)pc.Current)) ;
			return true;
		}
		internal static void SkipCommentsAndWhitespace(LexContext pc)
		{
			while (-1 != pc.Current)
				if (!_SkipWhiteSpace(pc) && !pc.TrySkipCLineComment())
					break;
			_SkipWhiteSpace(pc);
		}
	}
}
