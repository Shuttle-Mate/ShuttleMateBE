using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.ChatModelView;
using ShuttleMate.ModelViews.HistoryTicketModelView;
using ShuttleMate.Services.Services;
using System.Net.Http.Headers;
using System.Text;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }
    /// <summary>
    /// Lấy lịch sử đoạn chat.
    /// </summary>
    /// <param name="number">số từ 0, 1, 2,...: cứ tăng 1 đơn vị là lấy đoạn chat trong khung 24h về sau.</param>
    /// <param name="userId">Id của người dùng.</param>
    [HttpGet("history/{userId}")]
    public async Task<IActionResult> GetAndCleanChatHistory(int number,Guid userId)
    {
        var response = await _chatService.GetChatHistoryByTimeWindow(number, userId);

        return Ok(new BaseResponseModel<List<ChatHistoryResponse>>(
            statusCode: StatusCodes.Status200OK,
            code: ResponseCodeConstants.SUCCESS,
            data: response
        ));
    }
    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] ChatRequest model)
    {
        var response =  await _chatService.SendMessage(model);

        return Ok(new BaseResponseModel<ChatResponse>(
            statusCode: StatusCodes.Status200OK,
            code: ResponseCodeConstants.SUCCESS,
            data: response
        ));
    }

}
