using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace MyMonitorHub.Agent.Common
{
    public class WebApiCall
    {
        // EventPacket and Event use public fields rather than properties; IncludeFields
        // is required so System.Text.Json serializes them correctly.
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { IncludeFields = true };

        private readonly string _baseAddress;
        private readonly AgentTokenManager _tokenManager;

        public WebApiCall(string baseAddress, AgentTokenManager tokenManager)
        {
            _baseAddress = baseAddress;
            _tokenManager = tokenManager;
        }

        public TReturnObject Send<TReturnObject>(int accountId, int deviceId, object sendObject, string sendTo = "api/WebService/EventProcessor")
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(_baseAddress);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var accessToken = _tokenManager.GetAccessToken();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var query = $"{sendTo}?accountId={accountId}&deviceId={deviceId}";

                var response = client.PostAsJsonAsync(query, sendObject, _jsonOptions).GetAwaiter().GetResult();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    // Token may have been revoked — force re-auth by clearing cache and retrying once
                    MonitorProfile.Current.RefreshToken = null;
                    var freshToken = _tokenManager.GetAccessToken();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", freshToken);
                    response = client.PostAsJsonAsync(query, sendObject, _jsonOptions).GetAwaiter().GetResult();
                }

                if (response.IsSuccessStatusCode)
                {
                    var result = response.Content.ReadFromJsonAsync<TReturnObject>(_jsonOptions).GetAwaiter().GetResult();
                    return result!;
                }
                throw new WebApiCallException(response.ReasonPhrase);
            }
        }
    }
}
