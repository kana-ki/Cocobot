using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Cocobot.Utils
{
    internal class RollingCache<T> : IDisposable
    {

        private readonly ConcurrentDictionary<string, DateTime> _lastAccess = new();
        private readonly ConcurrentDictionary<string, T> _conversationContexts = new();
        private readonly Timer _timer;
        private bool _disposed;

        public RollingCache()
        {
            _timer = new Timer(_ => ExpireContexts(), null, 60_000, 60_000);
        }

        public T Get(string key)
        {
            _lastAccess[key] = DateTime.UtcNow;
            var exists = _conversationContexts.TryGetValue(key, out var value);
            return exists ? value : default;
        }

        public T GetOrSet(string key, T value)
        {
            _lastAccess[key] = DateTime.UtcNow;
            return _conversationContexts.GetOrAdd(key, value);
        }

        public T Remove(string key)
        {
            _lastAccess.TryRemove(key, out _);
            _conversationContexts.TryRemove(key, out var value);
            return value;
        }

        private void ExpireContexts()
        {
            foreach (var keyValuePair in _lastAccess)
            {
                if (keyValuePair.Value > DateTime.UtcNow.AddMinutes(-180))
                    continue;

                Remove(keyValuePair.Key);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
                _timer.Dispose();

            _disposed = true;
        }
    }
}
