namespace Yllibed.HttpServer.Defaults;
/// <summary>
/// OAuth Error Response standardized error codes. <see href="https://datatracker.ietf.org/doc/html/rfc6749#section-4.1.2.1">4.1.2.1 Error Response</see>
/// </summary>
public class OAuthErrorResponseDefaults
{
	public const string ErrorKey = "error";
	public const string ErrorDescriptionKey = "error_description";
	public const string ErrorUriKey = "error_uri";

	public const string AccessDenied = "access_denied";
	public const string InvalidRequest = "invalid_request";
	public const string UnauthorizedClient = "unauthorized_client";
	public const string InvalidClient = "invalid_client";
	public const string InvalidGrant = "invalid_grant";
	public const string UnsupportedGrantType = "unsupported_grant_type";
	public const string InvalidScope = "invalid_scope";
	public const string TemporarilyUnavailable = "temporarily_unavailable";
}
