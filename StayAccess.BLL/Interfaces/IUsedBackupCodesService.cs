using StayAccess.DAL.DomainEntities;
using StayAccess.DTO.Reservations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.BLL.Interfaces
{
    public interface IUsedBackupCodesService
    {
        Task NotifyBackupMarkedAsUsedAsync(int backupReservationId, string username);
        string ToCodeAsUsed(string oldReservationCode);
        ReservationRequestDto GetBackupReservationDtoToSetAsUsed(Reservation reservation);
        Task<(bool isValid, Reservation reservation)> IsArchesBackupReservation(int reservationId);
    }
}
