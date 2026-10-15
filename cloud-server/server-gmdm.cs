using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

class GmdmServer
{
    static string ROOT = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "game");
    static string PASSWORD = "gmdm2026";
    static int PORT = 62814;
    static string COOKIE = "gmdm_token=ok_" + (DateTime.UtcNow.Ticks / TimeSpan.TicksPerDay).ToString();

    static string PAGE = "<!DOCTYPE html><html lang=\"zh\"><head><meta charset=\"utf-8\">" +
        "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">" +
        "<title>\u8BBF\u95EE\u53E3\u4EE4</title><style>" +
        "body{background:#10141f;color:#e8e8f0;font-family:system-ui;display:flex;align-items:center;justify-content:center;height:100vh;margin:0}" +
        ".box{background:#1a2130;padding:40px;border-radius:16px;text-align:center;box-shadow:0 8px 30px #0006}" +
        "h1{font-size:18px;margin:0 0 20px}input{padding:10px 14px;border-radius:8px;border:1px solid #333;background:#10141f;color:#fff;font-size:16px;width:220px}" +
        "button{margin-top:16px;padding:10px 30px;border:0;border-radius:8px;background:#4f6ef7;color:#fff;font-size:15px;cursor:pointer}" +
        ".err{color:#ff7b7b;font-size:13px;margin-top:12px;min-height:16px}</style></head>" +
        "<body><form class=\"box\" method=\"post\" action=\"/login\">" +
        "<h1>\u8BF7\u8F93\u5165\u8BBF\u95EE\u53E3\u4EE4</h1><input type=\"password\" name=\"pwd\" autofocus>" +
        "<div class=\"err\">__ERR__</div><button type=\"submit\">\u8FDB\u5165</button></form></body></html>";

    static Dictionary<string, string> MIME = new Dictionary<string, string> {
        {".html", "text/html; charset=utf-8"}, {".js", "application/javascript"},
        {".wasm", "application/wasm"}, {".json", "application/json"},
        {".png", "image/png"}, {".jpg", "image/jpeg"}, {".jpeg", "image/jpeg"},
        {".ico", "image/x-icon"}, {".css", "text/css; charset=utf-8"},
        {".webmanifest", "application/manifest+json"}, {".svg", "image/svg+xml"},
        {".txt", "text/plain; charset=utf-8"}, {".gz", "application/octet-stream"}
    };

    // pre-compressed cache for the big Unity files (transfer-layer gzip)
    static Dictionary<string, string> gzCache = new Dictionary<string, string>();
    static string CACHE_DIR = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cache");
    static string[] BIG_FILES = {
        @"Build\GoodbyeMyDragonMother_v_0_3_1.data.gz",
        @"Build\GoodbyeMyDragonMother_v_0_3_1.wasm.gz",
        @"Build\GoodbyeMyDragonMother_v_0_3_1.framework.js.gz"
    };

    static void BuildGzCache()
    {
        try { Directory.CreateDirectory(CACHE_DIR); } catch { }
        foreach (string rel in BIG_FILES)
        {
            try
            {
                string fp = Path.Combine(ROOT, rel);
                if (!File.Exists(fp)) continue;
                string cf = Path.Combine(CACHE_DIR, rel.Replace('\\', '_') + ".gzip");
                if (!File.Exists(cf) || new FileInfo(cf).Length == 0)
                {
                    using (FileStream fs = File.Create(cf))
                    using (System.IO.Compression.GZipStream gz = new System.IO.Compression.GZipStream(fs, System.IO.Compression.CompressionMode.Compress))
                    using (FileStream src = File.OpenRead(fp))
                    {
                        byte[] b = new byte[262144]; int n;
                        while ((n = src.Read(b, 0, b.Length)) > 0) gz.Write(b, 0, n);
                    }
                    try { File.AppendAllText(LOG, "[" + DateTime.Now.ToString("HH:mm:ss") + "] compressed " + rel + "\n"); } catch { }
                }
                lock (gzCache) gzCache[rel] = cf;
            }
            catch (Exception ex)
            {
                try { File.AppendAllText(LOG, "[" + DateTime.Now.ToString("HH:mm:ss") + "] cache fail " + rel + " " + ex.Message + "\n"); } catch { }
            }
        }
    }

    static string LOG = @"C:\Users\admin\AppData\Local\Temp\1\gmdm-server\server.log";

    static void Main(string[] args)
    {
        if (args.Length > 0) { int p; if (int.TryParse(args[0], out p)) PORT = p; }
        if (args.Length > 1) PASSWORD = args[1];
        TcpListener listener = new TcpListener(IPAddress.Any, PORT);
        try { listener.Start(); }
        catch (Exception ex)
        {
            try { File.AppendAllText(LOG, "[" + DateTime.Now.ToString("HH:mm:ss") + "] BIND FAIL: " + ex + "\n"); } catch { }
            return;
        }
        Console.WriteLine("GMDM private server on port " + PORT + " root=" + ROOT);
        Thread ct = new Thread(BuildGzCache);
        ct.IsBackground = true;
        ct.Start();
        while (true)
        {
            TcpClient c = null;
            try { c = listener.AcceptTcpClient(); }
            catch (Exception ex) { try { File.AppendAllText(LOG, "[" + DateTime.Now.ToString("HH:mm:ss") + "] ACCEPT FAIL: " + ex + "\n"); } catch { } break; }
            try { Console.Error.WriteLine("accepted"); } catch { }
            TcpClient cc = c;
            Thread t = new Thread(delegate() { Handle(cc); });
            t.IsBackground = true;
            t.Start();
        }
    }

    static void Handle(TcpClient c)
    {
        try
        {
            try { Console.Error.WriteLine("handle called"); } catch { }
            try { File.AppendAllText(LOG, "[" + DateTime.Now.ToString("HH:mm:ss") + "] handle called\n"); } catch { }
            c.ReceiveTimeout = 0;
            c.SendTimeout = 0;
            using (NetworkStream ns = c.GetStream())
            {
                byte[] buf = new byte[8192];
                int got = 0;
                string hdr = null;
                int bodyStart = -1;
                while (true)
                {
                    int n = ns.Read(buf, got, buf.Length - got);
                    if (n <= 0) { try { File.AppendAllText(LOG, "[" + DateTime.Now.ToString("HH:mm:ss") + "] read eof\n"); } catch { } break; }
                    got += n;
                    if (hdr == null)
                    {
                        string s = Encoding.ASCII.GetString(buf, 0, Math.Min(got, 65536));
                        int idx = s.IndexOf("\r\n\r\n");
                        if (idx >= 0) { hdr = s.Substring(0, idx); bodyStart = idx + 4; break; }
                        if (got >= 65536) break;
                    }
                }
                if (hdr == null)
                {
                    try { File.AppendAllText(LOG, "[" + DateTime.Now.ToString("HH:mm:ss") + "] no header, got=" + got + "\n"); } catch { }
                    return;
                }
                // parse request line + headers
                string[] lines = hdr.Split(new[] { "\r\n" }, StringSplitOptions.None);
                string[] req = lines[0].Split(' ');
                if (req.Length < 2) return;
                string method = req[0], path = req[1];
                if (path.Length > 1 && path[0] == '/') { int q = path.IndexOf('?'); if (q >= 0) path = path.Substring(0, q); }
                Dictionary<string, string> hd = new Dictionary<string, string>();
                for (int i = 1; i < lines.Length; i++)
                {
                    int ci = lines[i].IndexOf(':');
                    if (ci > 0) hd[lines[i].Substring(0, ci).Trim().ToLowerInvariant()] = lines[i].Substring(ci + 1).Trim();
                }
                string cookie = hd.ContainsKey("cookie") ? hd["cookie"] : "";
                bool ok = cookie.Contains(COOKIE);
                try { File.AppendAllText(LOG, "[" + DateTime.Now.ToString("HH:mm:ss") + "] REQ " + method + " " + path + "\n"); } catch { }

                if (method == "POST" && path == "/login")
                {
                    int cl = 0; int.TryParse(hd.ContainsKey("content-length") ? hd["content-length"] : "0", out cl);
                    string body = "";
                    if (cl > 0 && bodyStart >= 0)
                    {
                        int have = got - bodyStart;
                        string s = Encoding.ASCII.GetString(buf, 0, got);
                        body = s.Substring(bodyStart, Math.Min(cl, have));
                    }
                    if (body.Contains("pwd=" + PASSWORD))
                    {
                        Send(ns, "HTTP/1.1 303 Found\r\nLocation: /\r\nSet-Cookie: " + COOKIE + "; Path=/; Max-Age=604800; SameSite=Lax\r\nContent-Length: 0\r\nConnection: close\r\n\r\n", null, false);
                    }
                    else SendPage(ns, 200, PAGE.Replace("__ERR__", "\u53E3\u4EE4\u9519\u8BEF\uFF0C\u518D\u8BD5\u4E00\u6B21"));
                    return;
                }

                if (path == "/favicon.ico") { SendPage(ns, 404, "Not Found"); return; }
                if (!ok) { SendPage(ns, 200, PAGE.Replace("__ERR__", "")); return; }
                ServeFile(ns, path, hd);
            }
        }
        catch (Exception ex)
        {
            try { File.AppendAllText(LOG, "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + ex + "\n"); } catch { }
            try { Console.Error.WriteLine("HANDLE EX: " + ex); } catch { }
        }
        finally { try { c.Close(); } catch { } }
    }

    static void SendPage(NetworkStream ns, int code, string body)
    {
        byte[] b = Encoding.UTF8.GetBytes(body);
        string head = "HTTP/1.1 " + code + (code == 200 ? " OK" : " Found") + "\r\n" +
            "Content-Type: text/html; charset=utf-8\r\nContent-Length: " + b.Length + "\r\nCache-Control: no-store\r\nConnection: close\r\n\r\n";
        byte[] hb = Encoding.ASCII.GetBytes(head);
        ns.Write(hb, 0, hb.Length); ns.Write(b, 0, b.Length); ns.Flush();
    }

    static void ServeFile(NetworkStream ns, string path, Dictionary<string, string> hd)
    {
        if (path == "/") path = "/index.html";
        string rel = path.TrimStart('/').Replace('/', '\\');
        string fp = Path.Combine(ROOT, rel);
        if (!File.Exists(fp)) { SendPage(ns, 404, "Not Found"); return; }
        DateTime lm = File.GetLastWriteTimeUtc(fp);
        string lmStr = lm.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        string ims = hd.ContainsKey("if-modified-since") ? hd["if-modified-since"] : null;
        if (!string.IsNullOrEmpty(ims))
        {
            DateTime imsT;
            if (DateTime.TryParse(ims, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out imsT))
            {
                DateTime lmS = new DateTime(lm.Year, lm.Month, lm.Day, lm.Hour, lm.Minute, lm.Second, DateTimeKind.Utc);
                if (imsT >= lmS)
                {
                    byte[] hb304 = Encoding.ASCII.GetBytes("HTTP/1.1 304 Not Modified\r\nConnection: close\r\n\r\n");
                    ns.Write(hb304, 0, hb304.Length); ns.Flush();
                    return;
                }
            }
        }
        string ext = Path.GetExtension(fp).ToLowerInvariant();
        string ct = "application/octet-stream";
        if (rel.EndsWith(".wasm.gz", StringComparison.OrdinalIgnoreCase)) ct = "application/wasm";
        else if (MIME.ContainsKey(ext)) ct = MIME[ext];
        long size = new FileInfo(fp).Length;
        long start = 0, end = size - 1;
        int code = 200;
        string rangeHdr = hd.ContainsKey("range") ? hd["range"] : null;
        // transfer-layer gzip for big Unity files on full requests (no Range)
        if (string.IsNullOrEmpty(rangeHdr))
        {
            string cf = null;
            lock (gzCache) { if (gzCache.ContainsKey(rel)) cf = gzCache[rel]; }
            if (cf != null && File.Exists(cf))
            {
                long csize = new FileInfo(cf).Length;
                StringBuilder sb2 = new StringBuilder();
                sb2.Append("HTTP/1.1 200 OK\r\n");
                sb2.Append("Content-Type: ").Append(ct).Append("\r\n");
                sb2.Append("Content-Encoding: gzip\r\n");
                sb2.Append("Content-Length: ").Append(csize).Append("\r\n");
                sb2.Append("Last-Modified: ").Append(lmStr).Append("\r\n");
                sb2.Append("Cache-Control: no-cache\r\nConnection: close\r\n\r\n");
                byte[] hb2 = Encoding.ASCII.GetBytes(sb2.ToString());
                ns.Write(hb2, 0, hb2.Length);
                using (FileStream f = File.OpenRead(cf))
                {
                    byte[] chunk = new byte[262144];
                    long left = csize;
                    while (left > 0)
                    {
                        int n = f.Read(chunk, 0, (int)Math.Min(chunk.Length, left));
                        if (n <= 0) break;
                        ns.Write(chunk, 0, n);
                        left -= n;
                    }
                }
                ns.Flush();
                return;
            }
        }
        if (!string.IsNullOrEmpty(rangeHdr) && rangeHdr.StartsWith("bytes="))
        {
            string r = rangeHdr.Substring(6);
            string[] ab = r.Split('-');
            long a = 0, b = 0;
            if (ab[0].Length > 0) long.TryParse(ab[0], out a); else a = 0;
            if (ab.Length > 1 && ab[1].Length > 0) long.TryParse(ab[1], out b); else b = size - 1;
            start = a; end = Math.Min(b, size - 1); code = 206;
        }
        long len = end - start + 1;
        StringBuilder sb = new StringBuilder();
        sb.Append("HTTP/1.1 ").Append(code).Append(code == 206 ? " Partial Content\r\n" : " OK\r\n");
        sb.Append("Content-Type: ").Append(ct).Append("\r\n");
        sb.Append("Content-Length: ").Append(len).Append("\r\n");
        sb.Append("Accept-Ranges: bytes\r\nLast-Modified: ").Append(lmStr).Append("\r\nConnection: close\r\n");
        if (code == 206) sb.Append("Content-Range: bytes ").Append(start).Append('-').Append(end).Append('/').Append(size).Append("\r\n");
        sb.Append("Cache-Control: no-cache\r\n\r\n");
        byte[] hb = Encoding.ASCII.GetBytes(sb.ToString());
        ns.Write(hb, 0, hb.Length);
        using (FileStream f = File.OpenRead(fp))
        {
            f.Seek(start, SeekOrigin.Begin);
            byte[] chunk = new byte[262144];
            long left = len;
            while (left > 0)
            {
                int n = f.Read(chunk, 0, (int)Math.Min(chunk.Length, left));
                if (n <= 0) break;
                ns.Write(chunk, 0, n);
                left -= n;
            }
        }
        ns.Flush();
    }

    static void Send(NetworkStream ns, string raw, byte[] body, bool flush)
    {
        byte[] b = Encoding.ASCII.GetBytes(raw);
        ns.Write(b, 0, b.Length);
        if (body != null) ns.Write(body, 0, body.Length);
        ns.Flush();
    }
}
