import json
import PyPDF2
import os
import openai
from langchain.text_splitter import CharacterTextSplitter
from langchain_community.llms import OpenAI
from langchain_community.embeddings import OpenAIEmbeddings
from langchain_community.vectorstores import FAISS


# Load API key securely from environment variable
openai.api_key = os.getenv("OPENAI_API_KEY")

def extract_text_from_pdf(pdf_path):
    """Extracts text from a PDF file."""
    text = ""
    with open(pdf_path, "rb") as file:
        reader = PyPDF2.PdfReader(file)
        for page in reader.pages:
            text += page.extract_text() + "\n"
    return text

def generate_quiz_questions(topic, content, num_questions=5):
    """Generates quiz questions based on extracted content."""
    prompt = f"Generate {num_questions} multiple-choice questions on {topic} based on the following text:\n{content}\n" \
             "Each question should have 4 options with one correct answer. Format: JSON output with 'question', 'options', 'correct_answer'."
    response = openai.ChatCompletion.create(
        model="gpt-4",
        messages=[{"role": "system", "content": "You are an AI quiz generator."},
                  {"role": "user", "content": prompt}]
    )
    return json.loads(response["choices"][0]["message"]["content"])

def process_materials(json_metadata, pdf_files):
    """Processes course materials and generates quizzes."""
    if not os.path.exists(json_metadata):
        print(f"Error: {json_metadata} not found. Creating an empty file...")
        with open(json_metadata, "w") as file:
            file.write("{}")  # Empty JSON object
    
    with open(json_metadata, 'r') as file:
        metadata = json.load(file)
    
    quizzes = {}
    for topic in metadata.get("topics", []):  # Ensure "topics" exists
        content = ""
        for pdf_path in pdf_files:
            content += extract_text_from_pdf(pdf_path) + "\n"
        
        questions = generate_quiz_questions(topic, content)
        quizzes[topic] = questions
    
    return quizzes

def save_quiz_to_json(quiz_data, output_file):
    """Saves generated quiz data to a JSON file."""
    with open(output_file, 'w') as file:
        json.dump(quiz_data, file, indent=4)
    print(f"Quiz saved to {output_file}")

# Example Usage
pdf_files = ["lecture1.pdf", "lecture2.pdf"]
quiz_data = process_materials("metadata.json", pdf_files)
save_quiz_to_json(quiz_data, "quiz_output.json")
