using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.AuthModelViews
{
    public class ConfirmOTPModelView
    {
        public string Email { get; set; }
        public string OTP { get; set; }
    }
}
