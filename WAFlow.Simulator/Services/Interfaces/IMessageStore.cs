using WebApplication1.Models;

namespace WebApplication1.Services.Interfaces;

public interface IMessageStore
{
    void Add(Message message);                       
    IReadOnlyList<Message> GetAll();                 
    IReadOnlyList<Message> GetByUser(string userId); 
    void Clear();
}