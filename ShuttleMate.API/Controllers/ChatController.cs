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
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
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
