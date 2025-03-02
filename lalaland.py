import json
import pdfplumber
import spacy
from transformers import pipeline, AutoModelForSeq2SeqLM, AutoTokenizer
from sklearn.metrics.pairwise import cosine_similarity
import numpy as np
import gradio as gr

# ----------------------------
# STEP 1: Extract & Filter Content
# ----------------------------
def extract_text_from_pdf(pdf_path):
    text = ""
    with pdfplumber.open(pdf_path) as pdf:
        for page in pdf.pages:
            text += page.extract_text() + "\n"
    return text

def filter_content_by_subtopic(text, subtopics, nlp_model):
    doc = nlp_model(text)
    subtopic_titles = [st["title"] for st in subtopics]
    filtered_content = {st: [] for st in subtopic_titles}
    
    for sent in doc.sents:
        sent_vector = nlp_model(sent.text).vector.reshape(1, -1)
        for st_title in subtopic_titles:
            st_vector = nlp_model(st_title).vector.reshape(1, -1)
            similarity = cosine_similarity(sent_vector, st_vector)[0][0]
            if similarity > 0.6:  # Adjust threshold as needed
                filtered_content[st_title].append(sent.text)
    return filtered_content

# ----------------------------
# STEP 2: Generate Revision Questions
# ----------------------------
class QuestionGenerator:
    def __init__(self):
        self.qg_model = AutoModelForSeq2SeqLM.from_pretrained("valhalla/t5-small-qa-qg-hl")
        self.tokenizer = AutoTokenizer.from_pretrained("t5-base")
        self.qa_pipeline = pipeline("question-answering", model="deepset/roberta-base-squad2")
    
    def generate_question(self, context):
        input_text = f"generate revision question: {context}"
        inputs = self.tokenizer(input_text, return_tensors="pt", max_length=512, truncation=True)
        outputs = self.qg_model.generate(**inputs, max_length=128)
        return self.tokenizer.decode(outputs[0], skip_special_tokens=True)
    
    def extract_answer(self, context, question):
        return self.qa_pipeline(question=question, context=context)["answer"]

# ----------------------------
# STEP 3: Generate Distractors
# ----------------------------
class DistractorGenerator:
    def __init__(self):
        self.dg_pipeline = pipeline("text2text-generation", model="mrm8488/t5-base-finetuned-question-generation-ap")
    
    def generate_distractors(self, correct_answer, context, n=4):
        prompt = f"""
        Generate {n} plausible but incorrect answers similar to: "{correct_answer}". 
        Context: {context}
        Examples of bad answers:
        - Partially correct but incomplete
        - Common student misconceptions
        - Opposite of correct answer
        """
        return [res["generated_text"] for res in self.dg_pipeline(prompt, max_length=128, num_return_sequences=n)]

# ----------------------------
# STEP 4: Build Final JSON Output
# ----------------------------
def build_output_json(filtered_content, subtopics):
    qg = QuestionGenerator()
    dg = DistractorGenerator()
    
    output = {"mainTopic": {"subTopics": []}}
    
    for st in subtopics:
        st_title = st["title"]
        quizzes = []
        for context in filtered_content[st_title][:10]:  # Process first 10 sentences
            question = qg.generate_question(context)
            correct_answer = qg.extract_answer(context, question)
            distractors = dg.generate_distractors(correct_answer, context)
            
            quizzes.append({
                "question": question,
                "goodAnswer": correct_answer,
                "wrongAnswer_1": distractors[0],
                "wrongAnswer_2": distractors[1],
                "wrongAnswer_3": distractors[2],
                "wrongAnswer_4": distractors[3]
            })
        
        output["mainTopic"]["subTopics"].append({
            "title": st_title,
            "quizzes": quizzes
        })
    
    return output

# ----------------------------
# STEP 5: Gradio Interface
# ----------------------------
def process_files(json_file, pdf_files):
    # Load JSON structure
    with open(json_file.name) as f:
        subtopics = json.load(f)["mainTopic"]["subTopics"]
    
    # Extract text from all PDFs
    full_text = ""
    for pdf in pdf_files:
        full_text += extract_text_from_pdf(pdf.name)
    
    # Filter content
    nlp = spacy.load("en_core_web_lg")
    filtered = filter_content_by_subtopic(full_text, subtopics, nlp)
    
    # Generate output
    output = build_output_json(filtered, subtopics)
    return json.dumps(output, indent=2)

gr.Interface(
    fn=process_files,
    inputs=[
        gr.File(label="Upload Subtopic JSON"),
        gr.File(label="Upload PDFs", file_count="multiple")
    ],
    outputs=gr.JSON(label="Generated Quizzes")
).launch()
