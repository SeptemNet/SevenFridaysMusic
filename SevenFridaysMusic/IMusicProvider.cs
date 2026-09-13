using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SevenFridaysMusic
{
    public interface IMusicProvider
    {
        Task<IReadOnlyList<TrackInfo>> SearchAsync(string searchText, CancellationToken ct = default);
        Task<string?> DownloadAsync(string videoId, CancellationToken ct = default);
    }
}
