using WebApplication1.Models;

namespace WebApplication1.Services.Interfaces;

public interface IRegistry
{
    void Set(WebhookRegistration registration);     
    WebhookRegistration? Get();                      
    void Clear();
}