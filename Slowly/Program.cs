using OpenAI.Chat;
using System.Reflection.PortableExecutable;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
namespace Slowly
{
    internal class Program
    {
        private static readonly string apikeyFile = @"..\..\..\..\/apikeys/openai.txt";
        private static readonly string apiKey = File.ReadAllText(apikeyFile);
        private static readonly string pdfPath = "..\\..\\..\\..\\lecture1.pdf"; // Path to your PDF file
        static void Main(string[] args)
        {
            

            ChatClient client = new(model: "gpt-4o", apiKey: apiKey);
            string extractedText = ExtractTextFromPdf(pdfPath);

            ChatCompletion completion = client.CompleteChat("summarise this:\n\n"+extractedText);

            Console.WriteLine($"[ASSISTANT]: {completion.Content[0].Text}");
        }
        static string ExtractTextFromPdf(string pdfPath)
        {
            using (PdfReader reader = new PdfReader(pdfPath))
            {
                StringWriter textWriter = new StringWriter();
                for (int i = 1; i <= reader.NumberOfPages; i++)
                {
                    string text = PdfTextExtractor.GetTextFromPage(reader, i);
                    textWriter.WriteLine(text);
                }
                return textWriter.ToString();
            }
        }
    }
}
