using OpenAI.Chat;
namespace Slowly
{
    internal class Program
    {
        private static readonly string apikeyFile = @"..\..\..\..\/apikeys/openai.txt";
        private static readonly string apiKey = File.ReadAllText(apikeyFile);
        static void Main(string[] args)
        {
            

            ChatClient client = new(model: "gpt-4o", apiKey: apiKey);

            ChatCompletion completion = client.CompleteChat("Say 'this is a test.'");

            Console.WriteLine($"[ASSISTANT]: {completion.Content[0].Text}");
        }
    }
}
