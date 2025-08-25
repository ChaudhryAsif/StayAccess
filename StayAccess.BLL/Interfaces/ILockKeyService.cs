using StayAccess.DAL.DomainEntities;
using StayAccess.DTO.Request.Latch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.BLL.Interfaces
{
    public interface ILockKeyService
    {
        /// <summary>
        /// add lock key
        /// </summary>
        /// <param name="lockKeyDto"></param>
        /// <param name="userName"></param>
        /// <returns></returns>
        public LockKey Add(LockKeyRequestDto lockKeyDto, string userName);

        /// <summary>
        /// update lock key
        /// </summary>
        /// <param name="lockKeyRequestDto"></param>
        /// <param name="userName"></param>
        /// <returns></returns>
        public Task UpdateAsync(LockKeyRequestDto lockKeyRequestDto, string userName);

        /// <summary>
        /// delete lock key
        /// </summary>
        /// <param name="lockKeyId"></param>
        /// <param name="userName"></param>
        /// <returns></returns>
        Task DeleteAsync(int lockKeyId, string userName);

       /// <summary>
       /// get by id lock key
       /// </summary>
       /// <param name="lockKeyId"></param>
       /// <returns></returns>
        Task<LockKey> GetByIdAsync(int lockKeyId);


        /// <summary>
        /// get matching lock key record from database
        /// </summary>
        /// <param name="buildingRequestDto"></param>
        /// <returns></returns>
        Task<LockKey> GetMatchedLockKeyAsync(LockKeyRequestDto lockKeyRequestDto);
    }
}
