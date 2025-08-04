using ShuttleMate.ModelViews.SchoolModelView;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.UserModelViews
{
    public class UserInforModel
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string ProfileImageUrl { get; set; }
        public DateTime? DateOfBirth { get; set; }
        //public int Balance { get; set; }
        public string? Address { get; set; }
        public bool Gender { get; set; }
        public ParentResponse? Parent { get; set; }
        public List<ChildResponse> Childs { get; set; }
        public SchoolResponse School { get; set; }
        public string RoleName { get; set; }
    }
}
