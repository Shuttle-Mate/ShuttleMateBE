using ShuttleMate.ModelViews.ChatModelView;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface IChatService
    {
        Task<ChatResponse> SendMessage(ChatRequest request);
        Task<List<ChatHistoryResponse>> GetChatHistoryByTimeWindow(int number, Guid userId);
    }
}
