using Microsoft.EntityFrameworkCore;
using StayAccess.DAL.DomainEntities;

namespace StayAccess.DAL
{
    public class StayAccessDbContext : DbContext
    {
        public StayAccessDbContext(DbContextOptions<StayAccessDbContext> options)
           : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.BuildingUnit)
                .WithMany(u => u.Reservations)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ReservationCode>()
                .HasOne(c => c.Reservation)
                .WithMany(r => r.ReservationCodes)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Building>(entity =>
            { 
                entity.HasIndex(b => b.Name).IsUnique();
            });

            modelBuilder.Entity<ReservationLatchData>(entity =>
            {
                entity.HasIndex(b => b.ReservationId).IsUnique();
            });

            modelBuilder.Entity<LockKey>(entity =>
            {
                entity.HasOne(l => l.Building).WithMany(b => b.LockKeys).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(l => l.BuildingUnit).WithMany(u => u.LockKeys).OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(l => new { l.UUid, l.BuildingUnitId }).IsUnique();
                entity.HasIndex(l => new { l.UUid, l.BuildingId }).IsUnique();
                entity.HasCheckConstraint("CK_LockKey_BuildingUnitId_or_BuildingId_Is_Null_And_Not_Both_Null",
                    "([BuildingUnitId] IS NULL OR [BuildingId] IS NULL) AND NOT ([BuildingUnitId] IS NULL AND [BuildingId] IS NULL)");

            });

        }

        public DbSet<ReservationLatchData> ReservationLatchData { get; set; }
        public DbSet<ReservationMCData> ReservationMCData { get; set; }
        public DbSet<Reservation> Reservation { get; set; }
        public DbSet<ReservationCode> ReservationCode { get; set; }
        public DbSet<LockKey> LockKey { get; set; }
        public DbSet<Building> Building { get; set; }
        public DbSet<BuildingUnit> BuildingUnit { get; set; }
        public DbSet<Logger> Logger { get; set; }
        public DbSet<UnitActionLog> UnitActionLog { get; set; }
        public DbSet<UnitLog> UnitLog { get; set; }
        public DbSet<UnitSlotLog> UnitSlotLog { get; set; }
        public DbSet<PersistentToken> PersistentToken { get; set; }
        public DbSet<CodeTransaction> CodeTransaction { get; set; }
        public DbSet<EventLogger> EventLogger { get; set; }
        public DbSet<EmailLogger> EmailLogger { get; set; }
        public DbSet<LatchAccessToken> LatchAccessToken { get; set; }
    }

}
