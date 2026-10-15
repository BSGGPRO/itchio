using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

class GetUrl
{
    static void Main()
    {
        // safety: force exit after 10 seconds no matter what
        using (new Timer(_ => Environment.Exit(0), null, 10000, Timeout.Infinite))
        {
            string dir = AppDomain.CurrentDomain.BaseDirectory;
            string log = Path.Combine(dir, "tunnel-url.log");
            string outFile = Path.Combine(dir, "url.txt");
            string content = "";
            try
            {
                if (File.Exists(log))
                {
                    using (FileStream fs = new FileStream(log, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        content = sr.ReadToEnd();
                    }
                }
            }
            catch { }
            Regex rx = new Regex(@"https://[a-z0-9-]+\.trycloudflare\.com");
            MatchCollection ms = rx.Matches(content);
            if (ms.Count > 0)
                File.WriteAllText(outFile, ms[ms.Count - 1].Value);
            else if (File.Exists(outFile))
                File.Delete(outFile);
        }
    }
}
