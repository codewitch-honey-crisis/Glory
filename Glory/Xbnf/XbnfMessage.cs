﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Glory
{
	
	public sealed class XbnfMessage : IMessage,IEquatable<XbnfMessage>,ICloneable
	{
		public XbnfMessage(ErrorLevel errorLevel, int errorCode, string message, int line, int column, long position,string fileOrUrl)
		{
			ErrorLevel = errorLevel;
			ErrorCode = errorCode;
			Message = message;
			Line = line;
			Column = column;
			Position = position;
			FileOrUrl = fileOrUrl;
		}
		public ErrorLevel ErrorLevel { get; private set; }
		public int ErrorCode { get; private set; }
		public string Message { get; private set; }
		public int Line { get; private set; }
		public int Column { get; private set; }
		public long Position { get; private set; }
		public string FileOrUrl { get; private set; }
		public override string ToString()
		{
			if (-1 == Position)
			{
				if (-1 != ErrorCode)
					return string.Format("{0}: {1} code {2}",
						ErrorLevel, Message, ErrorCode);
				return string.Format("{0}: {1}",
						ErrorLevel, Message);
			}
			else
			{
				var s = FileOrUrl;
				if (string.IsNullOrEmpty(s))
					s = "in-memory XBNF document";
				if (-1 != ErrorCode)
					return string.Format("{0}: {1} code {2} at line {3}, column {4}, position {5} in {6}",
						ErrorLevel, Message, ErrorCode, Line, Column, Position, s);
				return string.Format("{0}: {1} at line {2}, column {3}, position {4} in {5}",
						ErrorLevel, Message, Line, Column, Position, s);


			}
		}
		public XbnfMessage Clone()
		{
			return new XbnfMessage(ErrorLevel, ErrorCode, Message, Line, Column, Position,FileOrUrl);
		}
		object ICloneable.Clone()
			=> Clone();

		#region Value semantics
		public bool Equals(XbnfMessage rhs)
		{
			if (ReferenceEquals(rhs, this)) return true;
			if (ReferenceEquals(rhs, null)) return false;
			return ErrorLevel == rhs.ErrorLevel &&
				ErrorCode == rhs.ErrorCode &&
				Message == rhs.Message &&
				Line == rhs.Line &&
				Column == rhs.Column &&
				Position == rhs.Position &&
				FileOrUrl == rhs.FileOrUrl;
		}
		public override bool Equals(object rhs)
			=> Equals(rhs as XbnfMessage);

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
		public static bool operator ==(XbnfMessage lhs, XbnfMessage rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return true;
			if (ReferenceEquals(lhs, null)) return false;
			return lhs.Equals(rhs);
		}
		public static bool operator !=(XbnfMessage lhs, XbnfMessage rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return false;
			if (ReferenceEquals(lhs, null)) return true;
			return !lhs.Equals(rhs);
		}
		#endregion

	}
}
