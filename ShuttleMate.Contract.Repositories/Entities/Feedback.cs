using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShuttleMate.Contract.Repositories.Base;
using ShuttleMate.Core.Utils;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class Feedback : BaseEntity
    {
        //public enum FeedbackCategory { get; set; }
        public string Message { get; set; }
        public int Rating { get; set; }
        public string UserId { get; set; }
        public virtual User User { get; set; }
    }
}
