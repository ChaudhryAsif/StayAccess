using StayAccess.DAL.DomainEntities;
using StayAccess.DAL.Interfaces;
using StayAccess.Tools.Interfaces;
using System;
using System.Threading.Tasks;

namespace StayAccess.Tools.Repositories
{
    public class PersistentTokenService : IPersistentTokenService
    {
        private readonly IGenericService<PersistentToken> _persistentTokenRepo;

        public PersistentTokenService(IGenericService<PersistentToken> persistentTokenRepo)
        {
            _persistentTokenRepo = persistentTokenRepo;
        }

        public PersistentToken Add(PersistentToken persistentTokenDto)
        {
            try
            {
                PersistentToken persistentToken = new PersistentToken
                {
                    PersistentJwtToken = persistentTokenDto.PersistentJwtToken,
                    CreatedDate = DateTime.UtcNow
                };
                _persistentTokenRepo.AddWithSave(persistentToken);
                return persistentToken;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                PersistentToken persistentToken = await GetByIdAsync(id);

                if (persistentToken is null)
                    throw new Exception("Persistent Token not found.");

                _persistentTokenRepo.DeleteWithSave(persistentToken);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<PersistentToken> GetByIdAsync(int id)
        {
            try
            {
                return await _persistentTokenRepo.GetAsync(x => x.Id == id);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<PersistentToken> GetByTokenAsync(string token)
        {
            try
            {
                return await _persistentTokenRepo.GetAsync(x => x.PersistentJwtToken == token);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}