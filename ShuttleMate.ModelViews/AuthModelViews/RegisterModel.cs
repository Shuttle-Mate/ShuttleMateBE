using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.AuthModelViews
{
    public class RegisterModel
    {
        //public string Id { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public string Email { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string PlaceOfBirth { get; set; }
        public bool Gender { get; set; }
        public string? Address { get; set; }
        public string PhoneNumber { get; set; }

        //public bool RoleName { get; set; }
    }
}
