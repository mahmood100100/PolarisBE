namespace Polaris.Application.Common.Response
{
    public class ApiResponse
    {
        public int? StatusCode { get; set; }
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public object? Result { get; set; }

        public ApiResponse(int? StatusCode = null, string? Message = null, object? Result = null)
        {
            this.StatusCode = StatusCode;
            this.Message = Message ?? getMessagesForStatusCode(StatusCode);
            this.Result = Result;
            this.IsSuccess = StatusCode >= 200 && StatusCode <= 300;
        }

        private string? getMessagesForStatusCode(int? StatusCode)
        {
            return StatusCode switch
            {
                200 => "Success",
                201 => "Created Successfully",
                400 => "Bad Request",
                401 => "Unauthorized",
                404 => "Not Found",
                500 => "Internal Server Error",
                _ => null
            };
        }
    }
}
