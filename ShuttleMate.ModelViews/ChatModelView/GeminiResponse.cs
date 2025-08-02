using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.ChatModelView
{
    public class GeminiResponse
    {
        public List<GeminiCandidate> Candidates { get; set; }
    }
}
