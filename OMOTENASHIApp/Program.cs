using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using YamlLibrary;
using System.Collections.Generic;
using System.Web;
using System.Linq;
namespace OMOTENASHIApp
{
    class Program
    {

        static void Main(string[] args)
        {
            Setting setting = YamlObject.GetFromFile<Setting>(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "setting.yml"));
            var slack = new Slack(setting.Token);
            Dictionary<string, List<string>> message = setting.GetMessages();

            var directroy = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images"));

            Parallel.ForEach(directroy.EnumerateFiles("*", SearchOption.TopDirectoryOnly), new ParallelOptions() { MaxDegreeOfParallelism = 5 }, file =>
            {
                var user = file.Name.Replace(file.Extension, "");
                if (!message.ContainsKey(user))
                {
                    Console.WriteLine($"message is not found user: {file.Name}");
                    return;
                }

                var binary = ReadImageFile(file);

                var channel = slack.FileUpload(user, binary).Result;

                message[user].ForEach(x => slack.MessageSend(user, x, channel).Wait());
            });
        }

        private static byte[] ReadImageFile(FileInfo file)
        {
            using (var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
            using (var br = new BinaryReader(fs))
            {
                return br.ReadBytes((int)file.Length);
            }
        }

        private class Slack
        {
            private const string postMessageEndPoint = "chat.postMessage";
            private const string fileUploadEndPoint = "files.upload";

            private readonly HttpClient client;
            private readonly string token;

            public Slack(string token)
            {
                this.token = token;
                this.client = new HttpClient()
                {
                    BaseAddress = new Uri("https://slack.com/api/")
                };
                this.client.DefaultRequestHeaders.Add("Authorization", "Bearer " + this.token);
            }

            public async Task<string> FileUpload(string user, byte[] file)
            {
                var requestContent = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(file);
                requestContent.Add(fileContent, "file", "image.jpg");
                HttpResponseMessage response = await client.PostAsync(fileUploadEndPoint + $"?channels={user}", requestContent); //token={this.token}&

                var json = await response.Content.ReadAsStringAsync();
                dynamic jo = JsonConvert.DeserializeObject<dynamic>(json);
                if ((bool)jo.ok)
                {
                    Console.WriteLine($"file upload success user: {user} post channel: {user}");
                }
                else
                {
                    Console.WriteLine($"file upload error user: {user}");
                    Console.WriteLine(json);
                }

                return user;
            }

            public async Task MessageSend(string user, string message, string channel)
            {
                HttpResponseMessage response = await client.PostAsync(postMessageEndPoint + $"?token={this.token}&channel={channel}&as_user=true&text={HttpUtility.UrlEncode("```" + message + "```")}", new StringContent(""));

                var json = await response.Content.ReadAsStringAsync();
                dynamic jo = JsonConvert.DeserializeObject<dynamic>(json);

                if ((bool)jo.ok)
                {
                    Console.WriteLine($"message send success user: {user}");
                }
                else
                {
                    Console.WriteLine($"message send error user: {user}");
                    Console.WriteLine(json);
                }
            }
        }
    }
}
