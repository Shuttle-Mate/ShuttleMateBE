using ShuttleMate.Contract.Repositories.Base;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class Ward : BaseEntity
    {
        public string Name { get; set; }
        public virtual ICollection<Stop> Stops { get; set; } = new List<Stop>();
    }
}
