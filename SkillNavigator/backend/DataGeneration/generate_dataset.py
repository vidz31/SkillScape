import json
import random
import csv

doms = ['frontend', 'backend', 'fullstack', 'data', 'devops', 'ml', 'business', 'design', 'healthcare', 'education']

# More diverse and meaningful question contexts/scenarios
question_templates = [
    # Problem-solving scenarios
    "When faced with a performance problem, what would you tackle first?",
    "If a project needed optimization, which approach appeals to you most?",
    "When debugging an issue, what's your natural instinct?",
    "Working on a new feature, what aspect excites you most?",
    "If you had to choose a technical focus, which path interests you?",
    
    # Preference-based questions
    "Which of these work styles suits you best?",
    "What type of challenges do you find most engaging?",
    "In your ideal role, what would you spend the most time doing?",
    "What aspect of software development appeals to you most?",
    "Which skill would you most like to develop next?",
    
    # Impact-focused questions
    "How do you prefer to make an impact?",
    "What kind of problems excite you to solve?",
    "What outcome would make you feel most accomplished?",
    "Which area of technology fascinates you the most?",
    "What would you be most proud to build?",
    
    # Collaborative questions
    "In a team project, what role naturally fits you?",
    "When collaborating, where do you add the most value?",
    "What type of collaboration energizes you?",
    "Which team contribution motivates you most?",
    "In a professional setting, what's your strength?",
    
    # Learning and growth questions
    "What kind of learning challenges appeal to you?",
    "Which skill area would you like to master?",
    "What type of problem-solving engages your mind most?",
    "Which emerging technology excites you most?",
    "What would make your work feel most meaningful?",
]

option_templates = {
    'frontend': ["building interactive user interfaces", "crafting pixel-perfect designs", "creating smooth animations and interactions", "optimizing web app performance", "developing responsive mobile experiences", "enhancing user experience with engaging components", "implementing modern CSS frameworks", "building accessible web applications", "creating compelling visual interactions"],
    'backend': ["designing robust database architectures", "building scalable APIs", "implementing secure authentication systems", "optimizing server performance", "managing complex business logic", "implementing microservices", "handling data processing pipelines", "designing system architecture", "ensuring data integrity and security"],
    'fullstack': ["building complete applications from scratch", "connecting frontend and backend seamlessly", "managing full project lifecycle", "architecting end-to-end solutions", "handling cross-stack optimization", "building feature-complete products", "deploying and maintaining applications", "designing scalable system infrastructure", "orchestrating all layers of an application"],
    'data': ["analyzing patterns in large datasets", "building predictive models", "creating data visualizations", "extracting actionable business insights", "designing data pipelines", "performing statistical analysis", "optimizing database queries", "uncovering hidden trends", "driving decisions with data"],
    'devops': ["automating deployment processes", "managing cloud infrastructure", "monitoring system health and performance", "setting up CI/CD pipelines", "scaling applications for production", "ensuring system reliability", "implementing infrastructure-as-code", "managing containerized systems", "optimizing operational workflows"],
    'ml': ["training machine learning models", "implementing computer vision solutions", "processing natural language", "optimizing model accuracy", "researching advanced algorithms", "designing neural network architectures", "solving AI challenges", "experimenting with cutting-edge techniques", "evaluating model fairness and bias"],
    'business': ["identifying market opportunities", "analyzing business strategy", "managing projects and timelines", "building financial models", "negotiating and partnerships", "driving business growth", "optimizing costs and efficiency", "presenting strategic recommendations", "evaluating ROI and risks"],
    'design': ["creating beautiful user experiences", "designing intuitive interfaces", "building design systems", "conducting user research", "crafting visual brand identity", "prototyping innovative designs", "creating design documentation", "ensuring design consistency", "improving usability"],
    'healthcare': ["providing patient care and treatment", "analyzing medical data", "researching new treatments", "managing patient information systems", "improving healthcare delivery", "supporting clinical decisions", "building health tech solutions", "promoting patient wellness", "advancing medical knowledge"],
    'education': ["developing educational content", "teaching and mentoring", "designing learning experiences", "evaluating learning outcomes", "creating educational technology", "inspiring learners", "building curriculum frameworks", "fostering critical thinking", "advancing educational methods"]
}

questions = []
q_id = 1

# Generate 150 questions by cycling through question templates
while q_id <= 150:
    # Cycle through question templates
    template_index = (q_id - 1) % len(question_templates)
    question_text = question_templates[template_index]
    
    # Pick 4 random domains to be the options for this question
    chosen_domains = random.sample(doms, 4)
    options = []
    
    for i, domain in enumerate(chosen_domains):
        opt_text = random.choice(option_templates[domain])
        options.append({
            "id": f"q{q_id}_opt{i+1}",
            "text": opt_text,
            "domain": domain
        })
        
    questions.append({
        "id": f"q{q_id}",
        "text": question_text,
        "category": "General",
        "options": options
    })
    q_id += 1

# Save questions to JSON
with open('quiz_150_questions.json', 'w') as f:
    json.dump(questions, f, indent=2)

print(f"Generated {len(questions)} distinct questions.")

# Generate synthetic training data
# We'll simulate 5000 users. Each user will get 10 random questions from the 150 pool.
# Since ML models usually need fixed feature inputs, and a user only answers 10/150 questions,
# we need a unified feature space. The easiest way is that the features are the COUNT 
# of times the user selected each domain.
# Since there are 10 domains, the features are: frontend_count, backend_count...
# Wait, if we use those as features, it's just a simple max function.
# To make it a proper ML problem that learns weights (e.g., maybe 'building ML models' is highly predictive of ML, but 'analyzing data' is data),
# we should make the features the ACTUAL options selected.
# If we have 150 questions, each with 4 options, that's 600 possible categorical features.
# A user answers 10 questions, so 10 features are 1, 590 are 0.
# We will create a dataset where columns are q1_opt1, q1_opt2, ... q150_opt4, and Label is domain.

# Wait, a simpler ML approach:
# Just output the 10 selected question IDs and Option IDs, but for standard ML.NET it's easier to use text classification
# or a vector. Let's output a sparse vector or just a string of the text they chose!
# If we concatenate the text of the 10 chosen options into one block of text, we can use ML.NET Text Classification!
# This is a very robust way to handle variable arrays of questions.

dataset = []
for _ in range(5000):
    # User has a true underlying domain
    user_true_domain = random.choice(doms)
    
    # User is asked 10 random questions
    asked_questions = random.sample(questions, 10)
    
    chosen_texts = []
    
    for q in asked_questions:
        # User has an 80% chance of picking the option that matches their true domain (if available)
        # otherwise picks randomly
        matching_opts = [o for o in q['options'] if o['domain'] == user_true_domain]
        if matching_opts and random.random() < 0.8:
            selected = random.choice(matching_opts)
        elif matching_opts and random.random() < 0.9:
            # Pick a related domain 
            selected = random.choice(q['options'])
        else:
            selected = random.choice(q['options'])
            
        chosen_texts.append(selected['text'])
        
    combined_text = " ".join(chosen_texts)
    dataset.append([combined_text, user_true_domain])

# Write to CSV
with open('quiz_training_data.csv', 'w', newline='', encoding='utf-8') as f:
    writer = csv.writer(f)
    writer.writerow(["CombinedAnswers", "RecommendedDomain"])
    writer.writerows(dataset)

print(f"Generated 5000 synthetic responses in quiz_training_data.csv.")
