package mymonitorhub.agent.common;

import com.fasterxml.jackson.databind.ObjectMapper;
import org.apache.http.client.methods.CloseableHttpResponse;
import org.apache.http.client.methods.HttpPost;
import org.apache.http.entity.StringEntity;
import org.apache.http.impl.client.CloseableHttpClient;
import org.apache.http.util.EntityUtils;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.IOException;

/**
 * Sends JSON payloads to the hub over HTTPS with Bearer token auth.
 * Mirrors C# WebApiCall.
 */
public class WebApiCall {

    private static final Logger log = LoggerFactory.getLogger(WebApiCall.class);
    private static final ObjectMapper MAPPER = new ObjectMapper();

    private final String baseAddress;
    private final AgentTokenManager tokenManager;

    public WebApiCall(String baseAddress, AgentTokenManager tokenManager) {
        this.baseAddress = baseAddress;
        this.tokenManager = tokenManager;
    }

    /**
     * Serializes {@code payload} to JSON and POSTs it to
     * {@code api/WebService/EventProcessor?accountId=…&deviceId=…}.
     * Retries once with a fresh token on HTTP 401.
     */
    public void send(int accountId, int deviceId, Object payload) throws IOException {
        send(accountId, deviceId, payload, "api/WebService/EventProcessor");
    }

    public void send(int accountId, int deviceId, Object payload, String endpoint) throws IOException {
        boolean trustAll = MonitorProfile.getCurrent().isSslTrustAll();
        String body = MAPPER.writeValueAsString(payload);
        String url = baseAddress + endpoint + "?accountId=" + accountId + "&deviceId=" + deviceId;

        String token = tokenManager.getAccessToken();
        int statusCode = doPost(url, body, token, trustAll);

        if (statusCode == 401) {
            MonitorProfile.getCurrent().setRefreshToken(null);
            token = tokenManager.getAccessToken();
            statusCode = doPost(url, body, token, trustAll);
        }

        if (statusCode < 200 || statusCode >= 300) {
            throw new WebApiCallException("Unexpected HTTP status " + statusCode + " from " + endpoint);
        }
    }

    private int doPost(String url, String body, String token, boolean trustAll) throws IOException {
        try (CloseableHttpClient client = HttpClientFactory.create(trustAll)) {
            HttpPost post = new HttpPost(url);
            post.setHeader("Content-Type", "application/json");
            post.setHeader("Accept", "application/json");
            post.setHeader("Authorization", "Bearer " + token);
            post.setEntity(new StringEntity(body, "UTF-8"));

            try (CloseableHttpResponse response = client.execute(post)) {
                EntityUtils.consume(response.getEntity());
                return response.getStatusLine().getStatusCode();
            }
        }
    }
}
