using ShuttleMate.Contract.Repositories.Base;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class School : BaseEntity
    {
        public string Name { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public DateOnly? StartSemOne { get; set; }
        public DateOnly? EndSemOne { get; set; }
        public DateOnly? StartSemTwo { get; set; }
        public DateOnly? EndSemTwo { get; set; }
        public TimeOnly? MorningStartTime { get; set; }
        public TimeOnly? MorningEndTime { get; set; }
        public TimeOnly? AfternoonStartTime { get; set; }
        public TimeOnly? AfternoonEndTime { get; set; }
        public bool IsActive { get; set; } = true;
        public virtual ICollection<User> Students { get; set; } = new List<User>();
        public virtual ICollection<Route> Routes { get; set; } = new List<Route>();
    }
}
