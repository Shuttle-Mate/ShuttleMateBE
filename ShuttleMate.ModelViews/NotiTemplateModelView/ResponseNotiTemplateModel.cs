using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.NotiTemplateModelView
{
    public class ResponseNotiTemplateModel
    {
        public Guid Id { get; set; }
        public string Type { get; set; }
        public string Template { get; set; }
    }
}
