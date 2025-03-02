using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using OpenAI.API;
using OpenAI;
using System.Drawing;
using SkiaSharp;

namespace Extract_and_prompting
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string pdfPath = "..\\..\\..\\..\\lecture1.pdf"; // Path to your PDF file
            string extractedText = ExtractTextFromPdf(pdfPath);
            Console.WriteLine("Extracted Text:\n" + extractedText);

            // Extract images
            ExtractImagesFromPdf(pdfPath, "outputImages");

            // Interact with OpenAI's ChatGPT
            string prompt = "Based on the following text, provide a summary:\n\n" + extractedText;
            //string chatGptResponse = await GetChatGptResponse(prompt);
            //Console.WriteLine("ChatGPT Response:\n" + chatGptResponse);
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

        static void ExtractImagesFromPdf(string pdfPath, string outputFolder)
        {
            using (PdfReader reader = new PdfReader(pdfPath))
            {
                for (int i = 1; i <= reader.NumberOfPages; i++)
                {
                    PdfDictionary pageDict = reader.GetPageN(i);
                    PdfDictionary resources = (PdfDictionary)PdfReader.GetPdfObject(pageDict.Get(PdfName.RESOURCES));
                    PdfDictionary xObject = (PdfDictionary)PdfReader.GetPdfObject(resources.Get(PdfName.XOBJECT));
                    if (xObject != null)
                    {
                        foreach (PdfName name in xObject.Keys)
                        {
                            PdfObject obj = xObject.Get(name);
                            if (obj.IsIndirect())
                            {
                                PdfDictionary tg = (PdfDictionary)PdfReader.GetPdfObject(obj);
                                PdfName subtype = (PdfName)PdfReader.GetPdfObject(tg.Get(PdfName.SUBTYPE));
                                if (PdfName.IMAGE.Equals(subtype))
                                {
                                    int XrefIndex = Convert.ToInt32(((PRIndirectReference)obj).Number.ToString(System.Globalization.CultureInfo.InvariantCulture));
                                    PdfObject pdfObj = reader.GetPdfObject(XrefIndex);
                                    PdfStream pdfStrem = (PdfStream)pdfObj;
                                    byte[] bytes = PdfReader.GetStreamBytesRaw((PRStream)pdfStrem);
                                    if ((bytes != null))
                                    {
                                        using (MemoryStream memStream = new MemoryStream(bytes))
                                        {
                                            SKBitmap bitmap = SKBitmap.Decode(memStream);
                                            if (!Directory.Exists(outputFolder))
                                                Directory.CreateDirectory(outputFolder);
                                            string path = System.IO.Path.Combine(outputFolder, $"Image_Page_{i}_{name}.png");
                                            using (var image = SKImage.FromBitmap(bitmap))
                                            using (var data = image.Encode(SKEncodedImageFormat.Png, 100)) // 100 = max quality
                                            using (var stream = File.OpenWrite(path))
                                            {
                                                data.SaveTo(stream);
                                            }
                                            Console.WriteLine($"Image saved: {path}");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        /*
        static async Task<string> GetChatGptResponse(string prompt)
        {
            string apiKey = "YOUR_OPENAI_API_KEY"; // Replace with your OpenAI API key
            var openAiApi = new OpenAIAPI(apiKey);
            var chatRequest = new OpenAI.Chat
            {
                Model = OpenAI_API.Models.Model.ChatGPTTurbo,
                Temperature = 0.7,
                MaxTokens = 150,
                Messages = new[]
                {
                new OpenAI_API.Chat.ChatMessage
                {
                    Role = OpenAI_API.Chat.ChatMessageRole.System,
                    Content = "You are a helpful assistant."
                },
                new OpenAI_API.Chat.ChatMessage
                {
                    Role = OpenAI_API.Chat.ChatMessageRole.User,
                    Content = prompt
                }
            }
            };

            var chatResponse = await openAiApi.Chat.CreateChatCompletionAsync(chatRequest);
            return chatResponse.Choices[0].Message.Content.Trim();
        }
        */
    }
}