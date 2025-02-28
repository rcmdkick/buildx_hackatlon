using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ConsoleApp {
class OpenAIFileUploader
{
        private static readonly string apikeyFile = @"..\..\..\..\/apikeys/openai.txt";
        private static readonly string apiKey = File.ReadAllText(apikeyFile);
    private static readonly string uploadUrl = "https://api.openai.com/v1/files";

    public static async Task Main()
    {
        string filePath = @"..\..\..\..\/inputs/Anatomy🫁/Anatomy🫁 - Accessory digestive organs.pdf"; // Change this to your file path
        await UploadFileAsync(filePath);
    }

    public static async Task UploadFileAsync(string filePath)
    {
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            using (MultipartFormDataContent form = new MultipartFormDataContent())
            {
                form.Add(new StringContent("assistants"), "purpose"); // Use "fine-tune" if needed
                form.Add(new StreamContent(File.OpenRead(filePath)), "file", Path.GetFileName(filePath));

                HttpResponseMessage response = await client.PostAsync(uploadUrl, form);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    JObject jsonResponse = JObject.Parse(responseBody);
                    Console.WriteLine($"File uploaded successfully. File ID: {jsonResponse["id"]}");
                }
                else
                {
                    Console.WriteLine($"Error: {responseBody}");
                }
            }
        }
    }
}
}