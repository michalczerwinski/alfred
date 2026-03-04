using Alfred.Agent.Configuration;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Alfred.Agent.Tools;

public sealed class NotificationTool
{
	private static readonly HttpClient _httpClient = new();
	private readonly string _topic;

	public NotificationTool(NtfyOptions options)
	{
		_topic = options.Topic;
	}

	[KernelFunction("send_notification")]
	[Description("Sends a push notification to the user's device. Use this to inform the user when there is some urgent matter only. Do not abuse it")]
	public async Task<string> SendNotificationAsync(
		[Description("The message text to deliver as a notification.")] string message)
	{
		try
		{
			using var content = new StringContent(message);
			var response = await _httpClient.PostAsync($"https://ntfy.sh/{_topic}", content);

			return response.IsSuccessStatusCode
				? "Notification sent successfully."
				: $"Failed to send notification. Status: {(int)response.StatusCode} ({response.ReasonPhrase})";
		}
		catch (Exception ex)
		{
			return $"Error sending notification: {ex.Message}";
		}
	}
}
