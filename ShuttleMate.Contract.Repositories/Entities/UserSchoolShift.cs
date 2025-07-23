using ShuttleMate.Contract.Repositories.Base;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class UserSchoolShift : BaseEntity
    {
        public Guid SchoolShiftId { get; set; }
        public virtual SchoolShift SchoolShift { get; set; }
        public Guid StudentId { get; set; }
        public virtual User Student { get; set; }
    }
}
