using System.Net;

namespace CPDLCServer.Tests.Mocks;

public class TestHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _responses = new();
    private HttpResponseMessage? _defaultResponse;

    public List<HttpRequestMessage> Requests { get; } = new();
    public Dictionary<string, string> LastRequestFormData { get; private set; } = new();

    public void SetResponse(HttpStatusCode statusCode, string content)
    {
        _defaultResponse = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content)
        };
    }

    public void QueueResponse(HttpStatusCode statusCode, string content)
    {
        _responses.Enqueue(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content)
        });
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Requests.Add(request);

        if (request.Content is FormUrlEncodedContent formContent)
        {
            var formDataString = await formContent.ReadAsStringAsync(cancellationToken);
            LastRequestFormData = ParseFormData(formDataString);
        }

        if (_responses.TryDequeue(out var queuedResponse))
        {
            return queuedResponse;
        }

        return _defaultResponse ?? new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("ok")
        };
    }

    private Dictionary<string, string> ParseFormData(string formData)
    {
        var result = new Dictionary<string, string>();
        var pairs = formData.Split('&');

        foreach (var pair in pairs)
        {
            var keyValue = pair.Split('=', 2);
            if (keyValue.Length == 2)
            {
                result[Uri.UnescapeDataString(keyValue[0])] = Uri.UnescapeDataString(keyValue[1]);
            }
        }

        return result;
    }
}
