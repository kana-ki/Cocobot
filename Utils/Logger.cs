using Discord;
using System;
using System.Threading.Tasks;

namespace Cocobot.Utils
{
    internal class Logger : ILogger
    {
        public Task LogAsync(string type, string msg)
        {
            Console.WriteLine(new LogMessage(LogSeverity.Info, type, msg));
            return Task.CompletedTask;
        }

    }
}
