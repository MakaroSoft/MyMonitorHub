package mymonitorhub.agent.common;

import org.apache.http.conn.ssl.NoopHostnameVerifier;
import org.apache.http.conn.ssl.SSLConnectionSocketFactory;
import org.apache.http.conn.ssl.TrustAllStrategy;
import org.apache.http.impl.client.CloseableHttpClient;
import org.apache.http.impl.client.HttpClients;
import org.apache.http.ssl.SSLContextBuilder;

import javax.net.ssl.SSLContext;

/**
 * Produces HttpClient instances, optionally with SSL verification disabled.
 * Trust-all mode is intended for development against self-signed certificates only.
 */
public final class HttpClientFactory {

    private HttpClientFactory() {
    }

    public static CloseableHttpClient create(boolean trustAll) {
        if (!trustAll) {
            return HttpClients.createDefault();
        }
        try {
            SSLContext sslContext = SSLContextBuilder.create()
                    .loadTrustMaterial(null, TrustAllStrategy.INSTANCE)
                    .build();
            SSLConnectionSocketFactory csf = new SSLConnectionSocketFactory(
                    sslContext, NoopHostnameVerifier.INSTANCE);
            return HttpClients.custom()
                    .setSSLSocketFactory(csf)
                    .build();
        } catch (Exception e) {
            throw new RuntimeException("Failed to create trust-all HTTP client", e);
        }
    }
}
