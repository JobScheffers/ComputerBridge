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
            using (var sr = new StreamReader(fileStream))
            {
                string content = await sr.ReadToEndAsync();
                return PbnHelper.Load(content);
            }
        }

        public static async Task<Tournament> Load(string fileName)
        {
            Stream responseStream;
            if (fileName.StartsWith("http://"))
            {
                var url = new Uri(fileName);
                var myClient = new HttpClient(new HttpClientHandler() { UseDefaultCredentials = true });
                var response = await myClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                responseStream = await response.Content.ReadAsStreamAsync();
            }
            else
            {
                responseStream = File.OpenRead(fileName);
            }

            return await LoadAsync(responseStream);
        }

        public static void Save(Stream fileStream, Tournament tournament)
        {
            PbnHelper.Save(tournament, fileStream);
        }
    }
}
