using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDayWeb.Services
{
    public class ApiResult<T>
    {
        public bool Success { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public T? Data { get; set; }
        public string Error { get; set; } = "";
        public bool IsUnauthorised => StatusCode == HttpStatusCode.Unauthorized;
    }

    // The only class that talks to the RaceDay API. It adds the logged-in user's token to
    // every request and turns failures into friendly messages.
    public class ApiClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _http;
        private readonly IHttpContextAccessor _accessor;

        public ApiClient(HttpClient http, IHttpContextAccessor accessor)
        {
            _http = http;
            _accessor = accessor;
        }

        public Task<ApiResult<T>> GetAsync<T>(string url)
        {
            return SendAsync<T>(HttpMethod.Get, url, null);
        }

        public Task<ApiResult<T>> PostAsync<T>(string url, object body)
        {
            return SendAsync<T>(HttpMethod.Post, url, JsonContent.Create(body));
        }

        public Task<ApiResult<T>> PutAsync<T>(string url, object body)
        {
            return SendAsync<T>(HttpMethod.Put, url, JsonContent.Create(body));
        }

        public Task<ApiResult<T>> DeleteAsync<T>(string url)
        {
            return SendAsync<T>(HttpMethod.Delete, url, null);
        }

        // Sends an image to the API. The API, not this app, talks to Azure.
        public async Task<ApiResult<T>> PostFileAsync<T>(string url, IFormFile file)
        {
            using var stream = file.OpenReadStream();
            using var form = new MultipartFormDataContent();

            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            form.Add(fileContent, "file", file.FileName);

            return await SendAsync<T>(HttpMethod.Post, url, form);
        }

        private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, HttpContent? content)
        {
            try
            {
                using var request = new HttpRequestMessage(method, url);
                request.Content = content;

                string? token = _accessor.HttpContext?.Session.GetString(SessionKeys.Token);

                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                using var response = await _http.SendAsync(request);
                string body = await response.Content.ReadAsStringAsync();

                var result = new ApiResult<T>
                {
                    Success = response.IsSuccessStatusCode,
                    StatusCode = response.StatusCode
                };

                if (response.IsSuccessStatusCode)
                {
                    if (typeof(T) == typeof(string))
                    {
                        result.Data = (T)(object)body;
                    }
                    else if (!string.IsNullOrWhiteSpace(body))
                    {
                        result.Data = JsonSerializer.Deserialize<T>(body, JsonOptions);
                    }
                }
                else
                {
                    result.Error = BuildErrorMessage(response.StatusCode, body);
                }

                return result;
            }
            catch (HttpRequestException)
            {
                return Failure<T>("We couldn't reach the RaceDay service. Please try again in a moment.");
            }
            catch (TaskCanceledException)
            {
                return Failure<T>("The RaceDay service took too long to respond. Please try again.");
            }
            catch (JsonException)
            {
                return Failure<T>("We received an unexpected reply from the RaceDay service. Please try again.");
            }
        }

        private static ApiResult<T> Failure<T>(string message)
        {
            return new ApiResult<T>
            {
                Success = false,
                StatusCode = HttpStatusCode.ServiceUnavailable,
                Error = message
            };
        }

        private static string BuildErrorMessage(HttpStatusCode status, string body)
        {
            string trimmed = body.Trim();

            if (trimmed.StartsWith("{"))
            {
                string? validation = ReadValidationMessages(trimmed);

                if (validation != null)
                {
                    return validation;
                }
            }
            else if (trimmed.Length > 0 && trimmed.Length < 300 && !trimmed.StartsWith("<"))
            {
                // Plain-text messages written by our own API, such as "That email is already registered."
                return trimmed.Trim('"');
            }

            return status switch
            {
                HttpStatusCode.BadRequest => "Some of the information you entered isn't valid. Please check it and try again.",
                HttpStatusCode.Unauthorized => "Your session has expired. Please log in again.",
                HttpStatusCode.Forbidden => "You don't have permission to do that.",
                HttpStatusCode.NotFound => "We couldn't find what you were looking for.",
                HttpStatusCode.Conflict => "That change clashes with existing information.",
                _ when (int)status >= 500 => "Something went wrong on our side. Please try again in a moment.",
                _ => "Something went wrong. Please try again."
            };
        }

        // The API reports invalid input as a JSON object with an "errors" section
        private static string? ReadValidationMessages(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("errors", out var errors) || errors.ValueKind != JsonValueKind.Object)
                {
                    return null;
                }

                var messages = new List<string>();

                foreach (var field in errors.EnumerateObject())
                {
                    if (field.Name.StartsWith("$"))
                    {
                        messages.Add("Please check the information you entered.");
                        continue;
                    }

                    if (field.Value.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    foreach (var message in field.Value.EnumerateArray())
                    {
                        string? text = message.GetString();

                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            messages.Add(text);
                        }
                    }
                }

                return messages.Count > 0 ? string.Join(" ", messages.Distinct()) : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}