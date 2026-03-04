using System.ComponentModel;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.SemanticKernel;

namespace Alfred.Agent.Tools;

public sealed class WebFetchTool
{
    private static readonly HttpClient _httpClient = new(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(10)
    })
    {
        Timeout = TimeSpan.FromSeconds(30),
        DefaultRequestHeaders = { { "User-Agent", "Alfred-Agent/1.0" } }
    };

    [KernelFunction("get_web_page")]
    [Description("Fetches the content of a URL via HTTP GET and returns it as plain text. " +
                 "HTML pages are stripped to readable text. Useful for reading articles, " +
                 "documentation, APIs, or any publicly accessible web content.")]
    public async Task<string> GetWebPageAsync(
        [Description("The fully qualified URL to fetch, e.g. 'https://example.com/page'.")] string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return $"Invalid URL: '{url}'. Only http:// and https:// URLs are supported.";

        try
        {
            using var response = await _httpClient.GetAsync(uri);

            if (!response.IsSuccessStatusCode)
                return $"Request failed: {(int)response.StatusCode} {response.ReasonPhrase} — {url}";

            var content = await response.Content.ReadAsStringAsync();
            var mediaType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;

            return mediaType.Contains("html", StringComparison.OrdinalIgnoreCase)
                ? StripHtml(content)
                : content;
        }
        catch (TaskCanceledException)
        {
            return $"Request timed out fetching '{url}'.";
        }
        catch (Exception ex)
        {
            return $"Error fetching '{url}': {ex.Message}";
        }
    }

    private static string StripHtml(string html)
    {
        // Drop script and style blocks entirely
        html = Regex.Replace(html, @"<(script|style)[^>]*?>.*?</\1>", " ", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        // Replace block-level tags with newlines to preserve paragraph structure
        html = Regex.Replace(html, @"</(p|div|li|tr|h[1-6]|blockquote|br)\b[^>]*>", "\n", RegexOptions.IgnoreCase);
        // Strip remaining tags
        html = Regex.Replace(html, @"<[^>]+>", string.Empty);
        // Decode HTML entities (&amp; &lt; &nbsp; etc.)
        html = WebUtility.HtmlDecode(html);
        // Collapse runs of whitespace / blank lines
        html = Regex.Replace(html, @"[ \t]+", " ");
        html = Regex.Replace(html, @"\n{3,}", "\n\n");
        return html.Trim();
    }
}
