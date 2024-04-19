using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
namespace BusinessRules.Tests.Mocks
{
    public class MockHttpSession : ISession
    {
        public bool IsAvailable { get; set; }
        public string Id { get; set; }
        public IEnumerable<string> Keys { get; set; }
        private ConcurrentDictionary<string, object> SessionData { get; set; } = new();

        public MockHttpSession()
        {
            Id = Guid.NewGuid().ToString();
            Keys = new List<string>();
        }

        public void Clear()
        {
            return;
        }
        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                return;
            });
        }
        public Task LoadAsync(CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                return;
            });
        }
        public void Remove(string key)
        {
            return;
        }
        public void Set(string key, byte[] value)
        {
            SessionData.AddOrUpdate(key, value, (key, oldValue) => value);
            return;
        }
        public bool TryGetValue(string key, [NotNullWhen(true)] out byte[] value)
        {
            if (SessionData.TryGetValue(key, out var result))
            {
                value = result as byte[];
                return true;
            } else
            {
                value = Array.Empty<byte>();
                return false;
            }
        }
    }
}
