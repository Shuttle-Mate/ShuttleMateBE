using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.SchoolModelView
{
    public class ListStudentInSchoolResponse
    {
        public string? FullName { get; set; }
        public bool Gender { get; set; } = true;
        public DateTime? DateOfBirth { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string? Address { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ParentName { get; set; }
    }
}
