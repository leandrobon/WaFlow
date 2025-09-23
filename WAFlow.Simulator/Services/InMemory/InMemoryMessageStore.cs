using WebApplication1.Models;
using WebApplication1.Services.Interfaces;

namespace WebApplication1.Services.InMemory;

public sealed class InMemoryMessageStore : IMessageStore
{
    private readonly List<Message> _messages = new();
    private readonly object _sync = new();
    private readonly int _maxMessages;
    private long _seq = 0;

    public InMemoryMessageStore(int maxMessages = 5000)
    {
        _maxMessages = maxMessages;
    }
    
    public long LastSeq => Interlocked.Read(ref _seq);

    public void Add(Message message)
    {
        
        var next = Interlocked.Increment(ref _seq);
        message.Seq = next;
        
        lock (_sync)
        {
            _messages.Add(message);

            //Discard old messages if i hit the limit
            if (_messages.Count > _maxMessages)
            {
                int overflow = _messages.Count - _maxMessages;
                _messages.RemoveRange(0, overflow);
            }
        }
    }

    public IReadOnlyList<Message> GetAll()
    {
        lock (_sync)
        {
            return _messages.ToArray();
        }
    }

    public IReadOnlyList<Message> GetByUser(string userId)
    {
        lock (_sync)
        {
            return _messages
                .Where(m => string.Equals(m.From, userId, StringComparison.OrdinalIgnoreCase)
                            || string.Equals(m.To,   userId, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }
    }
    
    public IReadOnlyList<Message> GetSince(long seq)
    {
        lock (_sync)
            return _messages
                .Where(m => m.Seq > seq)
                .ToList();
    }
    
    public IReadOnlyList<Message> GetSince(string userId, long seq)
    {
        lock (_sync)
            return _messages
                .Where(m => m.Seq > seq && (m.From == userId || m.To == userId))
                .ToList();
    }

    public void Clear()
    {
        lock (_sync)
        {
            _messages.Clear();
        }
    }
}