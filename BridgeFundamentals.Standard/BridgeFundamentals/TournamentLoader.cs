using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Bridge
{
    [Obsolete("Replace with PbnHelper")]
    public static class TournamentLoader
    {
        /// <summary>
        /// Read a pbn file
        /// </summary>
        /// <param name="fileStream"></param>
        /// <returns></returns>
        public static async Task<Tournament> LoadAsync(Stream fileStream)
        {
            using var sr = new StreamReader(fileStream);
            string content = await sr.ReadToEndAsync().ConfigureAwait(false);
            return PbnHelper.Load(content);
        }

        public static async Task<Tournament> Load(string fileName)
        {
            Stream responseStream;
            if (fileName.StartsWith("http://"))
            {
                var url = new Uri(fileName);
                var myClient = new HttpClient(new HttpClientHandler() { UseDefaultCredentials = true });
                var response = await myClient.GetAsync(url).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            }
            else
            {
                responseStream = File.OpenRead(fileName);
            }

            return await LoadAsync(responseStream).ConfigureAwait(false);
        }

        public static void Save(Stream fileStream, Tournament tournament, string creator)
        {
            PbnHelper.Save(tournament, fileStream, creator);
        }
    }
}
