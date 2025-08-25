using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StayAccess.DAL.DomainEntities
{
    public class CodeTransaction : BaseEntity
    {
        [ForeignKey("Reservation")]
        [Required]
        public int? ReservationId { get; set; }

        [ForeignKey("ReservationCode")]
        public int? ReservationCodeId { get; set; }

        [Required]
        public string Unit { get; set; }

        public int FailedRetry { get; set; } = 0;

        [Required]
        public DoorType DoorType { get; set; }

        [Required]
        public TransactionAction Action { get; set; }

        [Required]
        public TransactionTriggerPoint TriggerPoint { get; set; }

        [Required]
        public TransactionStatus Status { get; set; }

        [Required]
        public DateTime ExecutionTime { get; set; }
        public string OldUnit { get; set; }
        public string ErrorMessage { get; set; }

        public virtual Reservation Reservation { get; set; }
        public virtual ReservationCode ReservationCode { get; set; }
    }

    public enum DoorType
    {
        ArchesFront = 1,
        ArchesUnit = 2,
        ArchesOldUnit = 3,
        Latch = 4,
        MC = 5
    }

    public enum TransactionAction
    {
        Create = 1,
        Update = 2,
        Delete = 3
    }

    public enum TransactionTriggerPoint
    {
        MoveIn = 1,
        MoveOut = 2,
        Now = 3
    }

    public enum TransactionStatus
    {
        Pending = 1,
        Executed = 2,
        Deleted = 3,
        FailedRetry = 4,
        InProcess = 5,
        Expired = 6,
        Completed = 7,
        Failed = 8,
    }
}
