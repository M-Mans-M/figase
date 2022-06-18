using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Figase.Utils
{
    public class HttpSender
    {
        private static readonly HttpClient client = new HttpClient();

        public static async Task<T> Send<T>(HttpMethod method, string url, object body = null)
        {
            var full = await SendFull<T>(method, url, body);
            return full.Item1;
        }

        public static async Task<(T, HttpStatusCode)> SendFull<T>(HttpMethod method, string url, object body = null)
        {
            var request = new HttpRequestMessage(method, url);
            if (body != null)
            {
                var json = JsonConvert.SerializeObject(body);
                var data = new StringContent(json, Encoding.UTF8, "application/json");
                request.Content = data;
            }

            var response = await client.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();
            
            var responseData = JsonConvert.DeserializeObject<T>(result);
            return (responseData, response.StatusCode);
        }

        public static async Task<HttpStatusCode> Send(HttpMethod method, string url, object body = null)
        {
            var request = new HttpRequestMessage(method, url);
            if (body != null)
            {
                var json = JsonConvert.SerializeObject(body);
                var data = new StringContent(json, Encoding.UTF8, "application/json");
                request.Content = data;
            }

            var response = await client.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();
            return response.StatusCode;
        }
    }
}
