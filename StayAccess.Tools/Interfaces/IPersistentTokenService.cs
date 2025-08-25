using StayAccess.DAL.DomainEntities;
using System.Threading.Tasks;

namespace StayAccess.Tools.Interfaces
{
    public interface IPersistentTokenService
    {
        PersistentToken Add(PersistentToken persistentTokenDto);
        Task DeleteAsync(int id);
        Task<PersistentToken> GetByIdAsync(int id);
        Task<PersistentToken> GetByTokenAsync(string token);
    }
}