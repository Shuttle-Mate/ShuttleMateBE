using ShuttleMate.Contract.Repositories.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.UserModelViews
{
    public class UpdateProfileModel
    {
        public string FullName { get; set; } = string.Empty;
        public bool Gender { get; set; } = true;
        public DateTime? DateOfBirth { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
    }
}
