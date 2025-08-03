using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.ChatModelView
{
    public class ChatPart
    {
        [JsonProperty("text")] // Chú ý chữ thường
        public string Text { get; set; }
    }
}
