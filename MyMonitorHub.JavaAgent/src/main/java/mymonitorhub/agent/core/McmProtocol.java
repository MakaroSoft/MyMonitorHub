package mymonitorhub.agent.core;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.Socket;
import java.nio.charset.Charset;

/**
 * MCM (Monitor Command Message) protocol framing — byte-for-byte compatible
 * with the C# McmProtocol so the Java agent and C# EventBroadcastClient
 * can interoperate.
 *
 * Wire format:
 *   mcm\r\n
 *   [Length: <n>\r\n]
 *   [other-header: value\r\n]*
 *   \r\n
 *   [body bytes if Length was present]
 */
public class McmProtocol {

    private static final Logger log = LoggerFactory.getLogger(McmProtocol.class);
    private static final int MAX_BODY_LENGTH = 10 * 1024 * 1024;

    private final Socket socket;
    private String header;

    public McmProtocol(Socket socket) {
        this.socket = socket;
    }

    public String getHeader() { return header; }

    // ── Write ──────────────────────────────────────────────────────────────────

    /**
     * @param params  header params — each must end with \r\n (e.g. "Status: OK\r\n")
     * @param data    optional body bytes; may be null
     */
    public void write(String params, byte[] data) throws IOException {
        if (params == null) params = "";
        String lengthLine = (data != null && data.length > 0)
                ? "Length: " + data.length + "\r\n" : "";
        String text = "mcm\r\n" + lengthLine + params + "\r\n";
        byte[] headerBytes = text.getBytes(Charset.forName("UTF-8"));

        OutputStream out = socket.getOutputStream();
        if (data != null && data.length > 0) {
            byte[] all = new byte[headerBytes.length + data.length];
            System.arraycopy(headerBytes, 0, all, 0, headerBytes.length);
            System.arraycopy(data, 0, all, headerBytes.length, data.length);
            out.write(all);
        } else {
            out.write(headerBytes);
        }
        out.flush();
    }

    // ── Read ───────────────────────────────────────────────────────────────────

    public byte[] read() throws IOException {
        InputStream in = socket.getInputStream();
        byte[] buf = new byte[1024];

        ByteArrayOutputStream mem = new ByteArrayOutputStream();
        byte[] mcmCheck = "mcm".getBytes(Charset.forName("UTF-8"));
        byte[] lookFor  = "\r\n\r\n".getBytes(Charset.forName("UTF-8"));

        boolean haveHeader = false;
        int headerEnd = 0;

        while (!haveHeader) {
            int len = in.read(buf);
            if (len < 0) throw new IOException("1: length is " + len);
            if (len > 0) {
                if (mem.size() == 0 && len > 2) {
                    // Verify MCM magic bytes on the very first chunk received
                    if (!startsWith(buf, mcmCheck)) {
                        throw new IOException("Not the mcm protocol");
                    }
                }
                mem.write(buf, 0, len);
                byte[] sofar = mem.toByteArray();
                int idx = findBytes(sofar, lookFor);
                if (idx != -1) {
                    headerEnd = idx;
                    haveHeader = true;
                }
            }
        }

        byte[] allBytes = mem.toByteArray();
        byte[] headerBytes = new byte[headerEnd + 4];
        System.arraycopy(allBytes, 0, headerBytes, 0, headerEnd + 4);
        header = new String(headerBytes, Charset.forName("UTF-8"));

        // Bytes after the header terminator
        ByteArrayOutputStream bodyBuf = new ByteArrayOutputStream();
        if (allBytes.length > headerEnd + 4) {
            bodyBuf.write(allBytes, headerEnd + 4, allBytes.length - (headerEnd + 4));
        }

        String lengthStr = getHeaderParam("Length: ");
        int bodyLength = 0;
        if (lengthStr != null) {
            try {
                bodyLength = Integer.parseInt(lengthStr.trim());
            } catch (NumberFormatException e) {
                throw new IOException("MCM body length parse error: " + lengthStr);
            }
            if (bodyLength < 0 || bodyLength > MAX_BODY_LENGTH) {
                throw new IOException("MCM body length out of range: " + bodyLength);
            }
        }

        while (bodyBuf.size() < bodyLength) {
            int len = in.read(buf);
            if (len <= 0) throw new IOException("2: length is " + len);
            bodyBuf.write(buf, 0, len);
        }

        if (!header.startsWith("mcm\r\n")) {
            throw new IOException("Unrecognized protocol");
        }
        return bodyBuf.toByteArray();
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private boolean startsWith(byte[] data, byte[] prefix) {
        if (data.length < prefix.length) return false;
        for (int i = 0; i < prefix.length; i++) {
            if (data[i] != prefix[i]) return false;
        }
        return true;
    }

    private int findBytes(byte[] haystack, byte[] needle) {
        outer:
        for (int i = 0; i <= haystack.length - needle.length; i++) {
            for (int j = 0; j < needle.length; j++) {
                if (haystack[i + j] != needle[j]) continue outer;
            }
            return i;
        }
        return -1;
    }

    public String getHeaderParam(String searchFor) {
        if (header == null) return null;
        String marker = "\r\n" + searchFor;
        int start = header.indexOf(marker);
        if (start == -1) return null;
        int valueStart = start + marker.length();
        int end = header.indexOf("\r\n", valueStart);
        if (end == -1) end = header.length();
        return header.substring(valueStart, end);
    }
}
