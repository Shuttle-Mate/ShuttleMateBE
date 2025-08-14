using ShuttleMate.Contract.Repositories.Base;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class Shuttle : BaseEntity
    {
        public string Name { get; set; }
        public string LicensePlate {  get; set; }
        public string VehicleType { get; set; }
        public string Color { get; set; }
        public int SeatCount { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public DateTime RegistrationDate { get; set; }
        public DateTime InspectionDate { get; set; }
        public DateTime NextInspectionDate { get; set; }
        public DateTime InsuranceExpiryDate { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsAvailable { get; set; } = true;
        public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
        public virtual ICollection<ScheduleOverride> OriginalScheduleOverrides { get; set; } = new List<ScheduleOverride>();
        public virtual ICollection<ScheduleOverride> OverrideScheduleOverrides { get; set; } = new List<ScheduleOverride>();
    }
}
