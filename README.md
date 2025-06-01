# 🤖🌐 AI-Powered Multilingual Chatbot with Azure OpenAI + Web Scraping

This project is a dynamic, multilingual chatbot application built using **ASP.NET Core MVC**, powered by **Azure OpenAI GPT-4o** and integrated with real-time **web scraping and content structuring**. It allows users to scrape a website, extract its content, and immediately interact with that content in either **English or Arabic** via a context-aware chatbot.

---

## 📦 Key Features

### 🕷️ Web Crawling & Scraping
- Users input a website URL via a simple UI.
- The app crawls and extracts the text content from that site.
- Extracted content is structured and persisted in a file.

### 💾 Persistent Context
- Structured data is saved to avoid re-scraping the website each session.
- A new URL triggers a full refresh, overwriting old data with the latest structured content.

### 🤖 GPT-4o Chatbot Integration
- Azure OpenAI GPT-4o is used via Microsoft's official SDK.
- Structured content acts as the chatbot’s system prompt/context.
- Each session uses updated content to answer questions intelligently.

### 🌐 Multilingual Support (English & Arabic)
- UI displays both English and Arabic previews of structured content.
- Buttons under each preview let users start a chatbot session in the selected language.
- The chatbot stays language-locked and context-aware in each session.

---

## ⚙️ Getting Started

### ✅ Prerequisites

- [.NET SDK 8.0+](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- An Azure OpenAI Resource with access to GPT-4o
- The following details from your Azure resource:
  - Endpoint
  - API Key
  - Deployment Name

### 📄 Configure `appsettings.json`

Replace placeholders with your actual Azure OpenAI credentials:

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://<your-resource-name>.openai.azure.com/",
    "ApiKey": "your-api-key",
    "DeploymentName": "gpt-4o"
  }
}
```

---

## 🛠️ 🛠️ Setup Instructions
Run the following commands to get started:

### 📥 1. Clone the Repository
git clone https://github.com/your-username/your-repo-name.git

### 2. Decopress bin.rar file

### 📂 3. Navigate to the Project Folder
cd Chatbot

### 🔧 4. Restore Dependencies
dotnet restore

### 🏃 5. Run the Application
dotnet run

The app will start on https://localhost:5001 or similar.

---


## 🌍 Real-World Use Case

This solution is ideal for organizations wanting to instantly convert their website into a **multilingual AI-powered assistant** without training or fine-tuning large language models. Some possible scenarios:

- ✅ Customer support agents powered by website content  
- ✅ Multilingual FAQ bots  
- ✅ Training assistants for internal documentation  
- ✅ Knowledge extraction from updated public content  

This approach ensures fast, consistent, and scalable interactions — always based on the most current data.


---

## 📚 Technologies Used

- **ASP.NET Core MVC**
- **Repository Pattern** 
- **Generic Repository**  
- **Unit Of Work** 
- **Azure OpenAI GPT-4o (via Microsoft.Azure.OpenAI SDK)**
- **HtmlAgilityPack** – for HTML parsing & scraping
- **System.IO** – for persistent storage
- **Razor Views** – styled multilingual frontend
- **C#** – for backend logic and API integration

---


## Demo
### HomePage
![Home-Page](/assets/HomePage.png)

### Wrong Url For WebScraping
![WebScraping Wrong Url](/assets/UI_ForScraping1.png)

### Correct Url For WebScraping
![WebScraping Correct Url](/assets/UI_ForScraping2.png)

### English Structured Content
![English Structured Content](/assets/English_StructuredContent.png)

### Arabic Structured Content
![Arabic Structured Content](/assets/Arabic_StructuredContent.png)

### English Chatbot
![English Chatbot](/assets/EnglishChat.png)

### Arabic Chatbot
![Arabic Chatbot](/assets/ArabicChat.png)

### Incorrect English Chat
![Incorrect English Chat](/assets/IncorrectQuestions_EnglishChat.png)

### Incorrect Arabic Chat
![Incorrect Arabic Chat](/assets/IncorrectQuestions_ArabicChat.png)



## 📽️ Full Demo Video (Google Drive)

### 📽️ Full Demo Video on Google Drive

If you’d like to see the app in action, check out videos:

👉 [Watch Full Demo on Google Drive](https://drive.google.com/drive/folders/12slERvqFIyeGLOtNv7om0XnlxSmwmLx5?usp=sharing)


---

## ✨ Summary

This chatbot solution combines **web scraping, content structuring**, and **Azure OpenAI GPT-4o** into a real-world-ready, multilingual application. Whether your audience speaks English or Arabic, the chatbot adapts intelligently to context — saving time, effort, and technical overhead.





