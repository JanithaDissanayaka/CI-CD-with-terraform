using Project_1.Models;

namespace Project_1.Data.Services
{
    public interface IBidsService
    {
        Task Add(Bid bid);
        IQueryable<Bid> GetAll();
        Task<string?> GetWonBidsAsync(string? userId);
    }
}