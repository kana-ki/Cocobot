using System.Threading.Tasks;

namespace Cocobot.Utils
{
    internal interface ILogger
    {
        Task LogAsync(string type, string msg);
    }
}