using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.ChatModelView
{
    public class GeminiContent
    {
        public List<ChatPart> Parts { get; set; }
        public string Role { get; set; }
    }
}
