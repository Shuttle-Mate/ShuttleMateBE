using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShuttleMate.Contract.Repositories.Base;
using ShuttleMate.Core.Utils;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class Feedback : BaseEntity
    {
        public FeedbackCategoryEnum FeedbackCategory { get; set; }
        public string Message { get; set; }
        public int Rating { get; set; }
        public Guid UserId { get; set; }
        public virtual User User { get; set; }
    }
}
