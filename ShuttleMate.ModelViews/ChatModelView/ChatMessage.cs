using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.ChatModelView
{
    public class ChatMessage
    {
        [JsonProperty("role")] // Chú ý chữ thường
        public string Role { get; set; }

        [JsonProperty("parts")] // Chú ý chữ thường
        public List<ChatPart> Parts { get; set; }
    }
}
