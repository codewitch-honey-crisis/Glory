using System;

namespace Glory
{
	/// <summary>
	/// Represents a message, such as a warning, error, or informational message from a grammar operation
	/// </summary>
#if CFGLIB
	public
#endif
		sealed class CfgMessage : IMessage, IEquatable<CfgMessage>, ICloneable
	{
		/// <summary>
		/// Constructs a new message with the specified parameters
		/// </summary>
		/// <param name="errorLevel">The error level</param>
		/// <param name="errorCode">An error code, or -1 for none</param>
		/// <param name="message">The message</param>
		/// <param name="line">The associated 1 based line, if any</param>
		/// <param name="column">The associated 1 based column, if any</param>
		/// <param name="position">The associated 0 based position, if any</param>
		/// <param name="fileOrUrl">The associated file or URL, if any</param>
		public CfgMessage(ErrorLevel errorLevel, int errorCode, string message, int line, int column, long position,string fileOrUrl)
		{
			ErrorLevel = errorLevel;
			ErrorCode = errorCode;
			Message = message;
			Line = line;
			Column = column;
			Position = position;
			FileOrUrl = fileOrUrl;
		}
		/// <summary>
		/// Indicates the error level
		/// </summary>
		public ErrorLevel ErrorLevel { get; private set; }
		/// <summary>
		/// Indicates the error code
		/// </summary>
		public int ErrorCode { get; private set; }
		/// <summary>
		/// Indicates the error message
		/// </summary>
		public string Message { get; private set; }
		/// <summary>
		/// Indicates the associated 1 based line
		/// </summary>
		public int Line { get; private set; }
		/// <summary>
		/// Indicates the associated 1 based column
		/// </summary>
		public int Column { get; private set; }
		/// <summary>
		/// Indicates the associated 0 based position
		/// </summary>
		public long Position { get; private set; }
		/// <summary>
		/// Indicates the associated file or URL
		/// </summary>
		public string FileOrUrl { get; private set; }
		/// <summary>
		/// Provides a string representation of the message
		/// </summary>
		/// <returns>An MSBUILD friendly message</returns>
		public override string ToString()
		{
			string el=null;
			switch(ErrorLevel)
			{
				case ErrorLevel.Error:
					el = "error";
					break;
				case ErrorLevel.Warning:
					el = "warning";
					break;
				case ErrorLevel.Information:
					el = "info";
					break;
			}
			return string.Format("{0}({1},{2}): {3} {4}: {5}",FileOrUrl,Line,Column,el,ErrorCode,Message);
		}
		/// <summary>
		/// Returns a deep copy of the message
		/// </summary>
		/// <returns>An equivelent message</returns>
		public CfgMessage Clone()
		{
			return new CfgMessage(ErrorLevel, ErrorCode, Message, Line, Column, Position,FileOrUrl);
		}
		object ICloneable.Clone()
			=> Clone();

		#region Value semantics
		public bool Equals(CfgMessage rhs)
		{
			if (ReferenceEquals(rhs, this)) return true;
			if (ReferenceEquals(rhs, null)) return false;
			return ErrorLevel == rhs.ErrorLevel &&
				ErrorCode == rhs.ErrorCode &&
				Message == rhs.Message &&
				Line == rhs.Line &&
				Column == rhs.Column &&
				Position == rhs.Position;
		}
		public override bool Equals(object rhs)
			=> Equals(rhs as CfgMessage);

		public override int GetHashCode()
		{
			var result = ErrorLevel.GetHashCode();
			result ^= ErrorCode;
			if (null != Message)
				result ^= Message.GetHashCode();
			result ^= Line;
			result ^= Column;
			result ^= Position.GetHashCode();
			return result;
		}
		public static bool operator ==(CfgMessage lhs, CfgMessage rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return true;
			if (ReferenceEquals(lhs, null)) return false;
			return lhs.Equals(rhs);
		}
		public static bool operator !=(CfgMessage lhs, CfgMessage rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return false;
			if (ReferenceEquals(lhs, null)) return true;
			return !lhs.Equals(rhs);
		}
		#endregion

	}
}
