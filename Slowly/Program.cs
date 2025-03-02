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
            string[] LEctures = Directory.GetDirectories(inputFolderPath);
            foreach (string Lecture in LEctures)
            {
                string LectureName = new DirectoryInfo(Lecture).Name;
                string outputPath = outputFolderPath+"/"+LectureName;
                string[] Subtopics = Directory.GetDirectories(Lecture);
                foreach (var subtopic in Subtopics)
                {
                    string folderName = new DirectoryInfo(subtopic).Name;
                    //string outputPath2 = outputPath+"/"+folderName;
                    ProcessSubtopic(subtopic,folderName,outputPath);
                }
                
            }

        }
        static string ExtractTextFromPdf(string folderpath)
        {
            string[] pdfpaths = FindSpecificFiles(folderpath,"*.pdf");
            string extracted_text = "";
            foreach (string file in pdfpaths)
            {
                try
                {
                    using (PdfReader reader = new PdfReader(file))
                    {
                        string text = string.Empty;
                        for (int page = 1; page <= reader.NumberOfPages; page++)
                        {
                            text += PdfTextExtractor.GetTextFromPage(reader, page);
                        }
                        extracted_text+= text;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred while processing {file}: {ex.Message}");
                }
            }
            return extracted_text;
        }
        static string GetInstructionPrompt()
        { 
        return File.ReadAllText(PromptPath);
        }
        static void ProcessSubtopic(string folderPath, string topicname,string outputpath)
        {
            ChatClient client = new(model: "gpt-4o", apiKey: apiKey);
            string extractedText = ExtractTextFromPdf(folderPath);
            string Metadata = GetMetadata(folderPath);
            CollectionResult<StreamingChatCompletionUpdate> completionUpdates = client.CompleteChatStreaming(GetInstructionPrompt() + "\n Use these topics: " + Metadata +"\n Here is the lecture itself: " + extractedText);
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
            if (!Directory.Exists(outputpath))
            {
                Directory.CreateDirectory(outputpath);
            }
            File.WriteAllText(System.IO.Path.Combine(outputpath,topicname)+".json", output);
        }
        static string[] FindSpecificFiles(string directoryPath, string extension)
        {
            try
            {
                // Define search patterns for PDF and JSON files
                string searchPattern = extension;


                // Retrieve files matching the current pattern
                string[] files = Directory.GetFiles(directoryPath, searchPattern, SearchOption.TopDirectoryOnly);
                foreach (string file in files)
                {
                    Console.WriteLine($"Found file: {file}");
                }
                return files;
                
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occurred: {e.Message}");
            }
            return null;
        }
        static string GetMetadata(string folder)
        {
            string metadataFile = FindSpecificFiles(folder, "*.json")[0];
            return File.ReadAllText(metadataFile);
        }
    }
}
