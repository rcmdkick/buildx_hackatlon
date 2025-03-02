using OpenAI.Chat;
using System.Reflection.PortableExecutable;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System.ClientModel;
namespace Slowly
{
    internal class Program
    {
        private static readonly string apikeyFile = @"..\..\..\..\/apikeys/openai.txt";
        private static readonly string apiKey = File.ReadAllText(apikeyFile);
        private static readonly string pdfPath = "..\\..\\..\\..\\/actual/lecture.pdf"; // Path to your PDF file
        private static readonly string PromptPath = "..\\..\\..\\..\\prompts.txt"; // Path to your prompt file
        private static readonly string outputPath = "..\\..\\..\\..\\/actual/output.json"; // Path to your prompt file
        private static readonly string metadataPath = "..\\..\\..\\..\\/actual/metadata.json"; // Path to your prompt file
        private static readonly string inputFolderPath = "..\\..\\..\\..\\/inputs";
        private static readonly string outputFolderPath = "..\\..\\..\\..\\/outputs";

        static void Main(string[] args)
        {
            string[] subdirectoryEntries = Directory.GetDirectories(inputFolderPath);
            foreach (string subdirectory in subdirectoryEntries)
            {
                string folderName = new DirectoryInfo(subdirectory).Name;
                ProcessSubtopic(subdirectory,folderName);
            }

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
        static string GetInstructionPrompt()
        { 
        return File.ReadAllText(PromptPath);
        }
        static void ProcessSubtopic(string folderPath, string topicname)
        {
            ChatClient client = new(model: "gpt-4o", apiKey: apiKey);
            string extractedText = ExtractTextFromPdf(pdfPath);

            CollectionResult<StreamingChatCompletionUpdate> completionUpdates = client.CompleteChatStreaming(GetInstructionPrompt() + "\n\n" + extractedText);
            string output = "";
            Console.Write($"[ASSISTANT]: ");
            foreach (StreamingChatCompletionUpdate completionUpdate in completionUpdates)
            {
                string newUpdate = "";
                if (completionUpdate.ContentUpdate.Count > 0)
                {
                    newUpdate += completionUpdate.ContentUpdate[0].Text;
                    Console.Write(newUpdate);
                    output += newUpdate;
                }
            }
            File.WriteAllText(folderPath+"/"+topicname+".json", output);
        }
        static string FindSpecificFile(string directoryPath, string extension)
        {
            try
            {
                // Define search patterns for PDF and JSON files
                string searchPattern = extension;


                    // Retrieve files matching the current pattern
                    string file = Directory.GetFiles(directoryPath,searchPattern)[0];

                    Console.WriteLine($"Found file: {file}");
                return file;
                
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occurred: {e.Message}");
            }
            return "";
        }
    }
}
