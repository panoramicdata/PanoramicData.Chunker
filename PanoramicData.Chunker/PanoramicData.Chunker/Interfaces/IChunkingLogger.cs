using Microsoft.Extensions.Logging;

namespace PanoramicData.Chunker.Interfaces;

/// <summary>
/// Interface for custom chunking logging integration.
/// Provides structured logging capabilities for the chunking process.
/// </summary>
public interface IChunkingLogger
{
	/// <summary>
	/// Logs a message with the specified log level.
	/// </summary>
	/// <param name="logLevel">The severity level of the log entry.</param>
	/// <param name="message">The log message.</param>
	void Log(LogLevel logLevel, string message);

	/// <summary>
	/// Logs a message with the specified log level and exception.
	/// </summary>
	/// <param name="logLevel">The severity level of the log entry.</param>
	/// <param name="exception">The exception to log.</param>
	/// <param name="message">The log message.</param>
	void Log(LogLevel logLevel, Exception exception, string message);

	/// <summary>
	/// Logs a message with structured data.
	/// </summary>
	/// <param name="logLevel">The severity level of the log entry.</param>
	/// <param name="message">The log message template.</param>
	/// <param name="args">The arguments for the message template.</param>
	void Log(LogLevel logLevel, string message, params object[] args);

	/// <summary>
	/// Determines if the specified log level is enabled.
	/// </summary>
	/// <param name="logLevel">The log level to check.</param>
	/// <returns>True if the log level is enabled; otherwise, false.</returns>
	bool IsEnabled(LogLevel logLevel);
}
