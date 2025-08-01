using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly string GeminiKey = "AIzaSyBqSgkHzsWIVGRhzxiBdujLoUimjM6ArPo";
    private readonly string SystemInstruction = @"
🏥 1. Thông tin chung về phòng khám

    Tên phòng khám: Phòng khám Đa khoa ABC

    Địa chỉ: Số 123, đường Nguyễn Huệ, TP Quảng Ngãi

    Số điện thoại: 0901 234 567

    Email: phongkhamabc@gmail.com

    Giờ làm việc:

        Thứ 2 - Thứ 7: 7h00 - 20h00

        Chủ nhật: 7h00 - 12h00

⚕️ 2. Danh sách dịch vụ khám chữa bệnh
STT	Tên dịch vụ	Giá tiền	Mô tả ngắn
1	Khám tổng quát	200.000đ	Kiểm tra toàn diện sức khỏe
2	Khám nội tổng quát	150.000đ	Chẩn đoán các bệnh lý nội khoa
3	Siêu âm bụng tổng quát	250.000đ	Phát hiện bất thường trong ổ bụng
4	Xét nghiệm máu cơ bản	180.000đ	Kiểm tra chỉ số máu thông thường
5	Khám tai mũi họng	150.000đ	Kiểm tra viêm xoang, viêm họng,...
6	Khám sản phụ khoa	250.000đ	Tư vấn, khám phụ khoa cho nữ giới
7	Khám nhi khoa	150.000đ	Khám cho trẻ em
👨‍⚕️ 3. Danh sách bác sĩ
Họ và tên	Chuyên khoa	Kinh nghiệm	Lịch làm việc
BS. Nguyễn Văn A	Nội tổng quát	15 năm	T2 - T7 (7h - 17h)
BS. Trần Thị B	Sản phụ khoa	10 năm	T2 - CN (7h - 20h)
BS. Lê Văn C	Tai Mũi Họng	12 năm	T2 - T7 (8h - 18h)
BS. Phạm Thị D	Nhi khoa	8 năm	T2 - CN (7h - 20h)
🔄 4. Chính sách đặt lịch & khám bệnh

    Đặt lịch: Qua điện thoại hoặc qua website (nếu có).

    Chính sách hủy lịch: Thông báo trước 24h.

    Khám không đặt lịch trước: Vẫn được phục vụ, nhưng có thể phải chờ.

    Thanh toán: Tiền mặt, chuyển khoản, hoặc qua ví điện tử (Momo, ZaloPay).";

    private readonly HttpClient _httpClient;

    public ChatController(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
    {
        try
        {
            var conversationHistory = await GetConversationHistory(request.SessionId);

            // Thêm tin nhắn mới với định dạng đúng
            conversationHistory.Add(new ChatMessage
            {
                Role = "user", // chữ thường
                Parts = new List<ChatPart> { new ChatPart { Text = request.Message } } // chữ thường
            });

            var requestData = new
            {
                system_instruction = new
                {
                    parts = new[]
                    {
                    new { text = SystemInstruction }
                }
                },
                contents = conversationHistory.Select(c => new
                {
                    role = c.Role,
                    parts = c.Parts.Select(p => new { text = p.Text })
                })
            };

            var response = await _httpClient.PostAsync(
                $"v1beta/models/gemini-1.5-flash:generateContent?key={GeminiKey}",
                new StringContent(JsonConvert.SerializeObject(requestData),
                Encoding.UTF8,
                "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var responseData = JsonConvert.DeserializeObject<GeminiResponse>(responseContent);

            if (responseData?.Candidates == null || responseData.Candidates.Count == 0)
            {
                return BadRequest("No response from AI model");
            }

            var aiResponse = responseData.Candidates[0].Content.Parts[0].Text;

            // Save the AI response to conversation history (you'll need to implement this)
            conversationHistory.Add(new ChatMessage
            {
                Role = "model",
                Parts = new List<ChatPart> { new ChatPart { Text = aiResponse } }
            });

            await SaveConversationHistory(request.SessionId, conversationHistory);

            return Ok(new ChatResponse
            {
                Response = aiResponse
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // Implement these methods based on your storage solution (database, cache, etc.)
    private async Task<List<ChatMessage>> GetConversationHistory(string sessionId)
    {
        // Retrieve conversation history from your storage
        // Return empty list if no history exists
        return new List<ChatMessage>();
    }

    private async Task SaveConversationHistory(string sessionId, List<ChatMessage> conversation)
    {
        // Save conversation history to your storage
    }
}

public class ChatRequest
{
    public string SessionId { get; set; } // To track conversation history
    public string Message { get; set; }
}

public class ChatResponse
{
    public string Response { get; set; }
}

public class ChatMessage
{
    [JsonProperty("role")] // Chú ý chữ thường
    public string Role { get; set; }

    [JsonProperty("parts")] // Chú ý chữ thường
    public List<ChatPart> Parts { get; set; }
}

public class ChatPart
{
    [JsonProperty("text")] // Chú ý chữ thường
    public string Text { get; set; }
}

public class GeminiResponse
{
    public List<GeminiCandidate> Candidates { get; set; }
}

public class GeminiCandidate
{
    public GeminiContent Content { get; set; }
}

public class GeminiContent
{
    public List<ChatPart> Parts { get; set; }
    public string Role { get; set; }
}