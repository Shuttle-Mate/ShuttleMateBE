using ShuttleMate.Core.Utils;

namespace ShuttleMate.Core.Constants
{
    public enum StatusCode
    {
        [CustomName("Success")]
        OK = 200,

        [CustomName("Bad Request")]
        BadRequest = 400,

        [CustomName("Unauthorized")]
        Unauthorized = 401,

        [CustomName("Not Found")]
        NotFound = 404,

        [CustomName("Internal Server Error")]
        ServerError = 500
    }
}
