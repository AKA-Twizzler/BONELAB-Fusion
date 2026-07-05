using MelonLoader;

using UnityEngine;

using System.Collections;
using System.Linq;

using LabFusion.Preferences;
using LabFusion.Utilities;

namespace LabFusion.Downloading.ModIO;

public static class ModIOThumbnailDownloader
{
    public static Dictionary<int, Texture> ThumbnailCache { get; } = new();

    public static void ClearCache()
    {
        foreach (var texture in ThumbnailCache.Values)
        {
            if (texture == null)
            {
                continue;
            }

            UnityEngine.Object.Destroy(texture);
        }

        ThumbnailCache.Clear();
    }

    public static void GetThumbnail(int modID, Action<Texture> callback)
    {
        if (modID <= 0) FusionLogger.Error($"GetThumbnail called with invalid modID: {modID}");
        if (ThumbnailCache.TryGetValue(modID, out var cachedTexture))
        {
            callback?.Invoke(cachedTexture);
            return;
        }

        callback += (texture) =>
        {
            ThumbnailCache[modID] = texture;
        };

        ModIOManager.GetMod(modID, OnModReceived);

        void OnModReceived(ModCallbackInfo info)
        {
            if (info.Result != ModResult.SUCCEEDED)
            {
                return;
            }

            // Check maturity
            if (info.Data.Mature && !CommonPreferences.ShowMatureMods)
            {
                return;
            }

            var url = info.Data.ThumbnailUrl;

            GetThumbnail(url, callback);
        }
    }

    public static void GetThumbnail(string url, Action<Texture> callback)
    {
        MelonCoroutines.Start(CoDownloadThumbnail(url, callback));
    }

    private static IEnumerator CoDownloadThumbnail(string url, Action<Texture> callback)
    {
        var handler = new System.Net.Http.SocketsHttpHandler
        {
            ConnectCallback = async (context, cancellationToken) =>
            {
                var hostEntry = await System.Net.Dns.GetHostEntryAsync(context.DnsEndPoint.Host, cancellationToken);
                var ipv4 = hostEntry.AddressList.First(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                var socket = new System.Net.Sockets.Socket(ipv4.AddressFamily, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp)
                {
                    NoDelay = true
                };
                await socket.ConnectAsync(new System.Net.IPEndPoint(ipv4, context.DnsEndPoint.Port), cancellationToken);
                return new System.Net.Sockets.NetworkStream(socket, ownsSocket: true);
            },
            ConnectTimeout = TimeSpan.FromSeconds(15)
        };

        using var client = new System.Net.Http.HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        var responseTask = client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

        while (!responseTask.IsCompleted)
        {
            yield return null;
        }

        var content = responseTask.Result.Content;

        var bytesTask = content.ReadAsByteArrayAsync();

        while (!bytesTask.IsCompleted)
        {
            yield return null;
        }

        var bytes = bytesTask.Result;

        var texture = new Texture2D(1, 1);

        ImageConversion.LoadImage(texture, bytes);

        callback?.Invoke(texture);
    }
}
