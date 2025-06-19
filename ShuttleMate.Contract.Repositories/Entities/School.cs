using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShuttleMate.Contract.Repositories.Base;
using ShuttleMate.Core.Utils;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class School : BaseEntity
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public TimeOnly SchoolTime { get; set; }
        public virtual ICollection<User> Users { get; set; } = new List<User>();
        public virtual ICollection<Route> Routes { get; set; } = new List<Route>();
    }
}
