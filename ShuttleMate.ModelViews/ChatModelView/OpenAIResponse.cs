using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.ChatModelView
{
    public class OpenAIResponse
    {
        public List<Choice> Choices { get; set; }
        public Usage Usage { get; set; }
    }

}
