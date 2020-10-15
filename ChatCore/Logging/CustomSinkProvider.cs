using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace ChatCore.Logging
{
	internal class CustomSinkProvider : ILoggerProvider
	{
		private readonly ChatCoreInstance _chatCoreInstance;
		private readonly ConcurrentDictionary<string, CustomLoggerSink> _loggers = new ConcurrentDictionary<string, CustomLoggerSink>();

		public CustomSinkProvider(ChatCoreInstance chatCoreInstance)
		{
			_chatCoreInstance = chatCoreInstance;
		}

		internal void OnLogReceived(CustomLogLevel level, string categoryName, string message)
		{
			_chatCoreInstance.OnLogReceivedInternal(level, categoryName, message);
		}

		public ILogger CreateLogger(string categoryName)
		{
			return _loggers.GetOrAdd(categoryName, _ => new CustomLoggerSink(this, categoryName));
		}

		public void Dispose()
		{
			_loggers.Clear();
		}
	}
}