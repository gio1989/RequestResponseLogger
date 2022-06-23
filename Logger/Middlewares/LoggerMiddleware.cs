using System.Text;
using Newtonsoft.Json;

namespace Logger.Middlewares
{
    public class LoggerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LoggerMiddleware> _logger;

        public LoggerMiddleware(RequestDelegate next, ILogger<LoggerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var requestUrl = $"{context.Request.Scheme} {context.Request.Host}{context.Request.Path} {context.Request.QueryString}";

            var tryParse = int.TryParse(context.Items["UserId"]?.ToString(), out var loginUserId);

            var formattedRequest = await FormatRequestAsync(context.Request, requestUrl, loginUserId);

            _logger.LogInformation(formattedRequest);

            //Copy a pointer to the original response body stream
            var originalBodyStream = context.Response.Body;

            //Create a new memory stream...
            await using var responseBody = new MemoryStream();
            //...and use that for the temporary response body
            context.Response.Body = responseBody;

            //Continue down the Middleware pipeline, eventually returning to this class
            await _next(context);

            //Format the response from the server
            var formattedResponse = await FormatResponseAsync(context.Response, requestUrl, loginUserId);

            _logger.LogInformation(formattedResponse);

            //Copy the contents of the new memory stream (which contains the response) to the original stream, which is then returned to the client.
            await responseBody.CopyToAsync(originalBodyStream);
        }

        private static async Task<string> FormatRequestAsync(HttpRequest request, string requestUrl, int loginUserId)
        {
            //This line allows us to set the reader for the request back at the beginning of its stream.
            request.EnableBuffering();

            //We now need to read the request stream.  First, we create a new byte[] with the same length as the request stream...
            var buffer = new byte[Convert.ToInt32(request.ContentLength)];

            //...Then we copy the entire request stream into the new buffer.
            await request.Body.ReadAsync(buffer).ConfigureAwait(false);

            //We convert the byte[] into a string using UTF8 encoding...
            var bodyAsText = Encoding.UTF8.GetString(buffer);

            request.Body.Position = 0;

            var requestObject = new RequestResponseModel(requestUrl, bodyAsText, loginUserId);

            var requestText = requestObject.ToString();

            return requestText;
        }

        private static async Task<string> FormatResponseAsync(HttpResponse response, string requestUrl, int loginUserId)
        {
            //We need to read the response stream from the beginning...
            response.Body.Seek(0, SeekOrigin.Begin);

            //...and copy it into a string
            var text = await new StreamReader(response.Body).ReadToEndAsync();

            //We need to reset the reader for the response so that the client can read it.
            response.Body.Seek(0, SeekOrigin.Begin);

            var responseObject = new RequestResponseModel(requestUrl, response.StatusCode, text, loginUserId)
            {
                Url = requestUrl,
                StatusCode = response.StatusCode,
                Response = text
            };

            var responseText = responseObject.ToString();

            //Return the string for the response, including the status code (e.g. 200, 404, 401, etc.)
            return responseText;
        }

        public class RequestResponseModel
        {
            public RequestResponseModel(string url, string request, int loginUserId)
            {
                Url = url;
                Request = request;
                LoginUserId = loginUserId;
            }

            public RequestResponseModel(string url, int statusCode, string response, int loginUserId)
            {
                Url = url;
                StatusCode = statusCode;
                Response = response;
                LoginUserId = loginUserId;
            }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string Url { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public int LoginUserId { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public int? StatusCode { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string? Request { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string? Response { get; set; }

            public override string ToString()
            {
                return JsonConvert.SerializeObject(this);
            }
        }
    }
}
