using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.AuthModelViews
{
    public class ConfirmEmailModel
    {
        public string Email { get; set; }
        public int Otp { get; set; }
    }
}
