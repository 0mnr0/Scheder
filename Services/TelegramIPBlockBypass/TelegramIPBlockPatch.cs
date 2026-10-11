using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Scheder.Tools.Config;

namespace Scheder.Services.TelegramIPBlockBypass;

public class TelegramIPBlockPatch {
    private const string Chain = "TG_REDIRECT";
    private const string Ports = "80,443";

    private static readonly string[] TargetDomains = [
        "my.telegram.org",
        "desktop.telegram.org",
        "macos.telegram.org",
        "oauth.telegram.org",
        "oauth.tg.dev",
        "cdn.telesco.pe",
        "cdn1.telesco.pe",
        "cdn2.telesco.pe",
        "cdn3.telesco.pe",
        "cdn4.telesco.pe",
        "cdn5.telesco.pe",
        "cdn6.telesco.pe",
        "core.telegram.org",
        "zws4.web.telegram.org",
        "vesta.web.telegram.org",
        "vesta-1.web.telegram.org",
        "venus-1.web.telegram.org",
        "telegram.me",
        "telegram.dog",
        "telegram.space",
        "telesco.pe",
        "tg.dev",
        "telegram.org",
        "t.me",
        "api.telegram.org",
        "td.telegram.org",
        "venus.web.telegram.org",
        "web.telegram.org",
        "webappinternal.telegram.org ",
        "kws2-1.web.telegram.org",
        "kws2.web.telegram.org",
        "kws4-1.web.telegram.org",
        "kws4.web.telegram.org",
        "zws2-1.web.telegram.org",
        "zws2.web.telegram.org",
        "zws4-1.web.telegram.org"
    ];
    
    

    private static int Ipt(string arguments, bool ignoreError = false)
    {
        var psi = new ProcessStartInfo("iptables", arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var p = Process.Start(psi)!;
        var err = p.StandardError.ReadToEnd();
        p.StandardOutput.ReadToEnd();
        p.WaitForExit();

        if (p.ExitCode != 0 && !ignoreError)
            throw new Exception($"iptables {arguments}\n{err}");

        return p.ExitCode;
    }
    
    // PUBLIC THINGS


    private static readonly string? TargetServerIp = Env.TransparentProxyIp;
    public static async Task<bool> Run() {
        if (TargetServerIp is null) {
            Cleanup();
            return false;
        }
        
        if (Environment.UserName != "root")
        {
            await Console.Error.WriteLineAsync("Нужны Linux и Root права!");
            return false;
        }
        
        var ips = new HashSet<string>();
        foreach (var domain in TargetDomains)
        {
            try
            {
                var addrs = await Dns.GetHostAddressesAsync(domain);
                foreach (var a in addrs.Where(a => a.AddressFamily == AddressFamily.InterNetwork))
                {
                    var ip = a.ToString();
                    if (ip == TargetServerIp) continue;
                    if (ips.Add(ip))
                        Console.WriteLine($"{domain} -> {ip}");
                }
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Не удалось разрешить {domain}: {ex.Message}");
            }
        }

        if (ips.Count == 0)
        {
            await Console.Error.WriteLineAsync("Не найдено ни одного IP, правила не изменены.");
            return false;
        }
        
        Ipt($"-t nat -N {Chain}", ignoreError: true);
        Ipt($"-t nat -F {Chain}");

        if (Ipt($"-t nat -C OUTPUT -j {Chain}", ignoreError: true) != 0)
            Ipt($"-t nat -A OUTPUT -j {Chain}");


        foreach (var ip in ips)
        {
            Ipt($"-t nat -A {Chain} -d {ip} -p tcp -m multiport --dports {Ports} " +
                $"-j DNAT --to-destination {TargetServerIp}");
        }

        Console.WriteLine($"Готово: {ips.Count} IP перенаправлены на {TargetServerIp}.");
        return true;
    }
    
    public static void Cleanup()
    {
        Ipt($"-t nat -D OUTPUT -j {Chain}", ignoreError: true);
        Ipt($"-t nat -D PREROUTING -j {Chain}", ignoreError: true);
        Ipt($"-t nat -F {Chain}", ignoreError: true);
        Ipt($"-t nat -X {Chain}", ignoreError: true);
    }
}