using ShuttleMate.Contract.Repositories.Base;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class SchoolShift : BaseEntity
    {
        public TimeOnly Time { get; set; }
        public ShiftTypeEnum ShiftType { get; set; }
        public SessionTypeEnum SessionType { get; set; }
        public Guid SchoolId { get; set; }
        public virtual School School { get; set; }
        public virtual ICollection<UserSchoolShift> UserSchoolShifts { get; set; } = new List<UserSchoolShift>();
    }
}
