using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShuttleMate.Contract.Repositories.Base;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class ScheduleOverride : BaseEntity
    {
        public Guid RouteId { get; set; }
        public Guid? OriginalUserId { get; set; }
        public Guid OverrideUserId { get; set; }
        public Guid ShuttleId { get; set; }
        public DateOnly Date { get; set; } //ngày áp dụng thay thế
        public TimeOnly? Time { get; set; } //thời gian áp dụng thay thế, nếu không có thì áp dụng cho cả ngày
        public string? Reason { get; set; }

        // Navigation Properties
        public virtual Route Route { get; set; } = null!;
        public virtual User? OriginalUser { get; set; }
        public virtual User OverrideUser { get; set; } = null!;
        public virtual Shuttle Shuttle { get; set; } = null!;
    }
}
