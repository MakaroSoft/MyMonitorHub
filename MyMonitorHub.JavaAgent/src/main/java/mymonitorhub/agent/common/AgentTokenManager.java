package mymonitorhub.agent.common;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.apache.http.client.methods.CloseableHttpResponse;
import org.apache.http.client.methods.HttpPost;
import org.apache.http.entity.StringEntity;
import org.apache.http.impl.client.CloseableHttpClient;
import org.apache.http.util.EntityUtils;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.IOException;
import java.nio.charset.Charset;
import java.util.Base64;
import java.util.Date;

/**
 * Manages the agent's access token lifecycle: authenticates with the API Key,
 * caches the access token in memory, proactively refreshes before expiry,
 * and persists the refresh token to MonitorProfile so it survives restarts.
 *
 * Thread-safe — multiple threads may call getAccessToken() concurrently.
 * Mirrors C# AgentTokenManager.
 */
public class AgentTokenManager {

    private static final Logger log = LoggerFactory.getLogger(AgentTokenManager.class);
    private static final ObjectMapper MAPPER = new ObjectMapper();
    private static final int REFRESH_BUFFER_MINUTES = 5;

    private final Object tokenLock = new Object();
    private String accessToken;
    private Date accessTokenExpiry = new Date(0);

    public AgentTokenManager() {
    }

    /**
     * Returns a valid access token, refreshing or re-authenticating as needed.
     * Blocks the calling thread if a token fetch is in progress.
     */
    public String getAccessToken() {
        synchronized (tokenLock) {
            if (isTokenValid()) {
                return accessToken;
            }

            MonitorProfile profile = MonitorProfile.getCurrent();

            if (profile.getRefreshToken() != null && !profile.getRefreshToken().isEmpty()) {
                TokenPair refreshed = tryRefresh(profile);
                if (refreshed != null) {
                    setToken(refreshed, profile);
                    return accessToken;
                }
                log.warn("getAccessToken> Refresh failed, falling back to API Key authentication.");
            }

            TokenPair pair = authenticate(profile);
            setToken(pair, profile);
            return accessToken;
        }
    }

    private boolean isTokenValid() {
        if (accessToken == null) return false;
        long bufferMs = (long) REFRESH_BUFFER_MINUTES * 60 * 1000;
        return System.currentTimeMillis() < (accessTokenExpiry.getTime() - bufferMs);
    }

    private void setToken(TokenPair pair, MonitorProfile profile) {
        this.accessToken = pair.accessToken;
        this.accessTokenExpiry = parseExpiry(pair.accessToken);

        if (!pair.refreshToken.equals(profile.getRefreshToken())) {
            profile.setRefreshToken(pair.refreshToken);
            profile.writeRefreshToken();
        }
    }

    private TokenPair authenticate(MonitorProfile profile) {
        log.info("getAccessToken> Authenticating with API Key.");
        try (CloseableHttpClient client = HttpClientFactory.create(profile.isSslTrustAll())) {
            String body = MAPPER.writeValueAsString(new AuthRequest(
                    profile.getAccountId(), profile.getDeviceId(), profile.getApiKey()));

            HttpPost post = new HttpPost(profile.getBaseAddress() + "api/agent/auth/token");
            post.setHeader("Content-Type", "application/json");
            post.setHeader("Accept", "application/json");
            post.setEntity(new StringEntity(body, "UTF-8"));

            try (CloseableHttpResponse response = client.execute(post)) {
                int status = response.getStatusLine().getStatusCode();
                String json = EntityUtils.toString(response.getEntity(), "UTF-8");
                if (status < 200 || status >= 300) {
                    throw new RuntimeException("Authentication failed: " + status + " " + json);
                }
                return parseTokenPair(json);
            }
        } catch (IOException e) {
            throw new RuntimeException("Authentication request failed: " + e.getMessage(), e);
        }
    }

    private TokenPair tryRefresh(MonitorProfile profile) {
        log.debug("getAccessToken> Refreshing token.");
        try (CloseableHttpClient client = HttpClientFactory.create(profile.isSslTrustAll())) {
            String body = "{\"refreshToken\":\"" + profile.getRefreshToken() + "\"}";

            HttpPost post = new HttpPost(profile.getBaseAddress() + "api/agent/auth/refresh");
            post.setHeader("Content-Type", "application/json");
            post.setHeader("Accept", "application/json");
            post.setEntity(new StringEntity(body, "UTF-8"));

            try (CloseableHttpResponse response = client.execute(post)) {
                int status = response.getStatusLine().getStatusCode();
                if (status < 200 || status >= 300) {
                    log.warn("getAccessToken> Token refresh returned {}", status);
                    return null;
                }
                String json = EntityUtils.toString(response.getEntity(), "UTF-8");
                return parseTokenPair(json);
            }
        } catch (Exception e) {
            log.error("getAccessToken> Token refresh threw an exception: {}", e.getMessage());
            return null;
        }
    }

    private TokenPair parseTokenPair(String json) throws IOException {
        JsonNode node = MAPPER.readTree(json);
        String at = node.path("accessToken").asText("");
        if (at.isEmpty()) at = node.path("AccessToken").asText("");
        String rt = node.path("refreshToken").asText("");
        if (rt.isEmpty()) rt = node.path("RefreshToken").asText("");
        return new TokenPair(at, rt);
    }

    /** Parses the JWT 'exp' claim without an external JWT library. */
    private Date parseExpiry(String jwt) {
        try {
            String[] parts = jwt.split("\\.");
            if (parts.length != 3) return defaultExpiry();

            String payload = parts[1];
            int mod = payload.length() % 4;
            if (mod != 0) {
                payload = payload + "====".substring(mod);
            }
            payload = payload.replace('-', '+').replace('_', '/');

            byte[] bytes = Base64.getDecoder().decode(payload);
            String payloadJson = new String(bytes, Charset.forName("UTF-8"));

            JsonNode node = MAPPER.readTree(payloadJson);
            if (node.has("exp")) {
                long unixSeconds = node.get("exp").asLong();
                return new Date(unixSeconds * 1000L);
            }
        } catch (Exception ignored) {
        }
        return defaultExpiry();
    }

    private Date defaultExpiry() {
        return new Date(System.currentTimeMillis() + 25L * 60 * 1000);
    }

    // ── Inner types ────────────────────────────────────────────────────────────

    private static class TokenPair {
        final String accessToken;
        final String refreshToken;

        TokenPair(String accessToken, String refreshToken) {
            this.accessToken = accessToken;
            this.refreshToken = refreshToken;
        }
    }

    @SuppressWarnings("unused")
    private static class AuthRequest {
        public final int accountId;
        public final int deviceId;
        public final String apiKey;

        AuthRequest(int accountId, int deviceId, String apiKey) {
            this.accountId = accountId;
            this.deviceId  = deviceId;
            this.apiKey    = apiKey;
        }
    }
}
