using StayAccess.DTO.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StayAccess.DAL.DomainEntities
{
    public class Reservation : BaseEntity
    {
        [ForeignKey("BuildingUnit")]
        [Required]
        public int BuildingUnitId { get; set; }

        public string Code { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime? EarlyCheckIn { get; set; }
        public DateTime? LateCheckOut { get; set; }
        public bool Cancelled { get; set; }
        public int? FrontDoorUserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public bool IsArchesNotificationSent { get; set; }  = false;
        public virtual BuildingUnit BuildingUnit { get; set; }

        public IList<ReservationCode> ReservationCodes { get; } = new List<ReservationCode>();
        //public IList<ReservationLatchData> ReservationLatchData { get; } = new List<ReservationLatchData>();
        public virtual ReservationLatchData ReservationLatchData { get; set; }
        public virtual ReservationMCData ReservationMCData { get; set; }

        public DateTime FromDate()
        {
            return EarlyCheckIn != null ? EarlyCheckIn.Value : StartDate;
        }

        public DateTime ToDate()
        {
            return LateCheckOut != null ? LateCheckOut.Value : EndDate;
        }

        /// <summary>
        /// Reservation's dates are valid. Currently during the reservation.
        /// </summary>
        /// <returns></returns>
        public bool IsCurrentActiveReservation()
        {
            DateTime? fromDate = FromDate();
            DateTime? toDate = ToDate();
            DateTime currentDateTime = DateTime.Now.Date;

            return !Cancelled
                && fromDate != null
                && DateTimeExtension.GetDateTime(currentDateTime, fromDate.Value.TimeOfDay.ToString()) //already started or starting now
                    >= fromDate.Value
                && DateTimeExtension.GetDateTime(currentDateTime, toDate.Value.TimeOfDay.ToString()) //in middle or ending now
                    <= toDate.Value;
        }

    }
}
