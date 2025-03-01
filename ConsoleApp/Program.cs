using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Net;

namespace ConsoleApp
{
    class OpenAIFileUploader
    {
        private static readonly string apikeyFile = @"..\..\..\..\/apikeys/openai.txt";
        private static readonly string apiKey = File.ReadAllText(apikeyFile);
        private static readonly string uploadUrl = "https://api.openai.com/v1/files";

        public static async Task Main()
        {
            string filePath = "..\\..\\..\\..\\\\inputs\\Anatomy\U0001fac1\\Anatomy\U0001fac1 - Accessory digestive organs\\Copy of Accessory digestive organs.pdf"; // Change this to your file path
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

                    HttpResponseMessage response_file = await client.PostAsync(uploadUrl, form);
                    string responseBody_file = await response_file.Content.ReadAsStringAsync();

                    if (response_file.IsSuccessStatusCode)
                    {
                        JObject jsonResponse = JObject.Parse(responseBody_file);
                        Console.WriteLine($"File uploaded successfully. File ID: {jsonResponse["id"]}");

                        var requestBody = new
                        {
                            model = "gpt-4-turbo",
                            messages = new[]
                            {
                            new { role = "system", content = "Use the uploaded file for context." },
                            new { role = "user", content = "Analyze the file and summarize its contents." }
                            },
                            file_ids = new[] { responseBody_file }
                        };

                        var json = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);
                        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                        HttpResponseMessage response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
                        string responseBody = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"Chat Response: {responseBody}");
                        }
                        else
                        {
                            Console.WriteLine($"Error: {responseBody}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Error: {responseBody_file}");
                    }
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);



                }
            }

        }
    }
}