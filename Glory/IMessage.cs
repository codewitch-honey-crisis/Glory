using System;
using System.Collections.Generic;
using System.Text;

namespace Glory
{
	/// <summary>
	/// Indicates the error level of a message
	/// </summary>
	public

		enum ErrorLevel
	{
		/// <summary>
		/// Indicates an informational message
		/// </summary>
		Information=0,
		/// <summary>
		/// Indicates a warning
		/// </summary>
		Warning = 1,
		/// <summary>
		/// Indicates an error
		/// </summary>
		Error=2
	}
	/// <summary>
	/// Represents an abstract interface for a message
	/// </summary>
	public

		interface IMessage
	{
		/// <summary>
		/// The error level
		/// </summary>
		ErrorLevel ErrorLevel { get;}
		/// <summary>
		/// The message string
		/// </summary>
		string Message { get; }
		/// <summary>
		/// The error code, or -1
		/// </summary>
		int ErrorCode { get; }
		/// <summary>
		/// The associated 1 based line, if any
		/// </summary>
		int Line { get; }
		/// <summary>
		/// The associated 1 based column, if any
		/// </summary>
		int Column { get; }
		/// <summary>
		/// The associated 0 based position, if any
		/// </summary>
		long Position { get; }
		/// <summary>
		/// The associated file or URL, if any
		/// </summary>
		string FileOrUrl { get; }
	}
}
