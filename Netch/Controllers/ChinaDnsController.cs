using Netch.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Netch.Controllers
{
    public class ChinaDnsController
    {
        public Process Process { get; set; }

        public LocalDnsUtil LocalDnsUtil { get; set; }

        public ChinaDnsController(string ip)
        {
            LocalDnsUtil = new LocalDnsUtil(ip);
        }

        public bool Start()
        {
            foreach (var proc in Process.GetProcessesByName("ts-dns"))
            {
                try
                {
                    proc.Kill();
                }
                catch (Exception)
                {
                    // 跳过
                }
            }

            if (!File.Exists("bin\\ts-dns.exe"))
            {
                return false;
            }

            Process = new Process();
            Process.StartInfo.WorkingDirectory = String.Format("{0}\\bin", Directory.GetCurrentDirectory());
            Process.StartInfo.FileName = String.Format("{0}\\bin\\ts-dns.exe", Directory.GetCurrentDirectory());
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("listen = \":53\"");
            stringBuilder.AppendLine(String.Format("gfwlist = \"{0}\"", Directory.GetCurrentDirectory()+"\\gfwlist.txt").Replace("\\","\\\\"));
            stringBuilder.AppendLine(String.Format("cnip = \"{0}\"", Directory.GetCurrentDirectory() + "\\cnip.txt").Replace("\\", "\\\\"));
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("[groups]");
            stringBuilder.AppendLine("  [groups.clean]");
            stringBuilder.AppendLine(String.Format("  dns = [\"114.114.115.115\", \"114.114.114.114\"]"));
            stringBuilder.AppendLine("  concurrent = true");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("  [groups.dirty]");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("  dns = [\"8.8.8.8\", \"8.8.4.4\"]");
            stringBuilder.AppendLine(String.Format("  socks5 = \"127.0.0.1:{0}\"", Global.Settings.Socks5LocalPort.ToString()));
            if (File.Exists(Directory.GetCurrentDirectory() + "\\ts-dns.toml")){
                File.Delete(Directory.GetCurrentDirectory() + "\\ts-dns.toml");
            }
            File.WriteAllText(Directory.GetCurrentDirectory() + "\\ts-dns.toml", stringBuilder.ToString());
            Process.StartInfo.Arguments = String.Format("-c \"{0}\"", Directory.GetCurrentDirectory() + "\\ts-dns.toml");
            Process.StartInfo.CreateNoWindow = true;
            Process.StartInfo.RedirectStandardError = true;
            Process.StartInfo.RedirectStandardInput = true;
            Process.StartInfo.RedirectStandardOutput = true;
            Process.StartInfo.UseShellExecute = false;
            Process.EnableRaisingEvents = true;
            Process.ErrorDataReceived += OnOutputDataReceived;
            Process.OutputDataReceived += OnOutputDataReceived;
            Process.Start();
            Process.BeginErrorReadLine();
            Process.BeginOutputReadLine();
            Process.PriorityClass = ProcessPriorityClass.RealTime;

            LocalDnsUtil.ChangeDns(new List<string>() { "127.0.0.1" });
            return true;
        }

        public void Stop()
        {
            try
            {
                if (Process != null && !Process.HasExited)
                {
                    Process.Kill();
                }

                //pDNSController.Stop();
                //修复点击停止按钮后再启动，DNS服务没监听的BUG
                LocalDnsUtil.RecoverDns();
            }
            catch (Exception e)
            {
                Utils.Logging.Info(e.ToString());
            }
        }

        public void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!String.IsNullOrEmpty(e.Data))
            {
                File.AppendAllText("logging\\ts-dns.log", String.Format("{0}\r\n", e.Data.Trim()));
            }
        }

    }
}
