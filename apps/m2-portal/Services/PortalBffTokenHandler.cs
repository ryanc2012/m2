using Microsoft.Identity.Web;

namespace M2Portal.Services;

public class PortalBffTokenHandler(ITokenAcquisition tokenAcquisition, IConfiguration config) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var scope = config["AzureAd:PortalBffScope"] ?? "api://m2-portal-bff/.default";
        try
        {
            var token = await tokenAcquisition.GetAccessTokenForUserAsync([scope]);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
        catch (MicrosoftIdentityWebChallengeUserException)
        {
            // Token acquisition failed — will redirect to login
        }
        catch (Exception)
        {
            // In dev with no real Azure AD, gracefully skip token injection
        }
        return await base.SendAsync(request, ct);
    }
}
