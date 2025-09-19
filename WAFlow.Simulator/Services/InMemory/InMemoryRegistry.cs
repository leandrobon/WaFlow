using WebApplication1.Models;
using WebApplication1.Services.Interfaces;

namespace WebApplication1.Services.InMemory;

public sealed class InMemoryRegistry : IRegistry
{
    private WebhookRegistration? _current;
    private readonly object _sync = new();

    public void Set(WebhookRegistration registration)
    {
        lock (_sync)
        {
            _current = registration;
        }
    }

    public WebhookRegistration? Get()
    {
        lock (_sync)
        {
            return _current;
        }
    }

    public void Clear()
    {
        lock (_sync)
        {
            _current = null;
        }
    }
}
