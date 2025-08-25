using Newtonsoft.Json;
using StayAccess.BLL.Interfaces;
using StayAccess.DAL.DomainEntities;
using StayAccess.DAL.Interfaces;
using StayAccess.DTO.Enums;
using StayAccess.DTO.Request;
using StayAccess.DTO.Request.Latch;
using StayAccess.Tools.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.BLL.Repositories
{
    public class LockKeyService : ILockKeyService
    {
        private readonly IGenericService<LockKey> _lockKeyRepo;
        private readonly ILoggerService<LockKeyService> _loggerRepo;
        public LockKeyService(IGenericService<LockKey> lockKeyRepo, ILoggerService<LockKeyService> loggerRepo)
        {
            _lockKeyRepo = lockKeyRepo;
            _loggerRepo = loggerRepo;
        }
        public LockKey Add(LockKeyRequestDto lockKeyDto, string userName)
        {
            try
            {
                if (lockKeyDto.BuildingId == 0)
                    lockKeyDto.BuildingId = null;
                if (lockKeyDto.BuildingUnitId == 0)
                    lockKeyDto.BuildingUnitId = null;


                LockKey lockKey = new LockKey
                {
                    Id = lockKeyDto.Id,
                    BuildingUnitId = lockKeyDto.BuildingUnitId,
                    BuildingId = lockKeyDto.BuildingId,
                    KeyId = lockKeyDto.KeyId,
                    Name = lockKeyDto.Name,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = userName,
                };
                
                _loggerRepo.Add(LogType.Information, $"Creating new lockKey: {JsonConvert.SerializeObject(lockKey)}.", null);
                _lockKeyRepo.AddWithSave(lockKey);
                _loggerRepo.Add(LogType.Information, $"New lockKey created successfully for: {JsonConvert.SerializeObject(lockKey)}.", null);

                return lockKey;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task UpdateAsync(LockKeyRequestDto lockKeyDto, string username)
        {
            try
            {
                LockKey lockKey = await GetByIdAsync(lockKeyDto.Id);

                if (lockKey is null)
                    throw new Exception("Lockkey not found");

                lockKey.Id = lockKeyDto.Id;
                lockKey.BuildingUnitId = lockKeyDto.BuildingUnitId;
                lockKey.BuildingId = lockKeyDto.BuildingId;
                lockKey.KeyId = lockKeyDto.KeyId;
                lockKey.Name = lockKeyDto.Name;
                lockKey.ModifiedDate = DateTime.UtcNow;
                lockKey.ModifiedBy = username;

                _loggerRepo.Add(LogType.Information, $"Updating lockKey: {JsonConvert.SerializeObject(lockKey)}.", null);
                _lockKeyRepo.UpdateWithSave(lockKey);
                _loggerRepo.Add(LogType.Information, $"LockKey updated successfully for: {JsonConvert.SerializeObject(lockKey)}.", null);
                
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<LockKey> GetByIdAsync(int buildingId)
        {
            try
            {
                return await _lockKeyRepo.GetAsync(x => x.Id == buildingId);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task DeleteAsync(int lockKeyId, string userName)
        {
            try
            {
                LockKey lockKey = await GetByIdAsync(lockKeyId);

                if (lockKey is null)
                    throw new Exception("Lockkey not found");

                _loggerRepo.Add(LogType.Information, $"Deleting lockKey: {JsonConvert.SerializeObject(lockKey)}.", null);
                _lockKeyRepo.DeleteWithSave(lockKey);
                _loggerRepo.Add(LogType.Information, $"LockKey deleted successfully for: {JsonConvert.SerializeObject(lockKey)}.", null);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<LockKey> GetMatchedLockKeyAsync(LockKeyRequestDto lockKeyRequestDto)
        {
            try
            {
                return await _lockKeyRepo.GetAsync(x => x.BuildingUnitId == lockKeyRequestDto.BuildingUnitId 
                                                        && x.BuildingId == lockKeyRequestDto.BuildingId
                                                        && x.Name == lockKeyRequestDto.Name
                                                        && x.KeyId == lockKeyRequestDto.KeyId);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
