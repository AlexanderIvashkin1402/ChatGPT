using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Completions;
using OpenAI_API.Models;
using OpenAI_API.Moderation;

var secretAppsettingReader = new SecretAppsettingReader.SecretAppsettingReader();
var chatGptApiKey = secretAppsettingReader.ReadSection<string>("ChatGPTApiKey", true, typeof(Program).Assembly);

OpenAIAPI api = new OpenAIAPI(new APIAuthentication(chatGptApiKey));

// Try #1
/*
var chat = api.Chat.CreateConversation();

/// give instruction as System
chat.AppendSystemMessage("You are a teacher who helps children understand if things are animals or not.  If the user tells you an animal, you say \"yes\".  If the user tells you something that is not an animal, you say \"no\".  You only ever respond with \"yes\" or \"no\".  You do not say anything else.");

// give a few examples as user and assistant
chat.AppendUserInput("Is this an animal? Cat");
chat.AppendExampleChatbotOutput("Yes");
chat.AppendUserInput("Is this an animal? House");
chat.AppendExampleChatbotOutput("No");

// now let's ask it a question'
chat.AppendUserInput("Is this an animal? Dog");
// and get the response
string response = await chat.GetResponseFromChatbotAsync();
Console.WriteLine(response); // "Yes"

// and continue the conversation by asking another
chat.AppendUserInput("Is this an animal? Chair");
// and get another response
response = await chat.GetResponseFromChatbotAsync();
Console.WriteLine(response); // "No"

// the entire chat history is available in chat.Messages
foreach (ChatMessage msg in chat.Messages)
{
    Console.WriteLine($"{msg.Role}: {msg.Content}");
}
*/

// Try #2
/*
var chat = api.Chat.CreateConversation();
chat.AppendUserInput("How to make a hamburger?");

await foreach (var res in chat.StreamResponseEnumerableFromChatbotAsync())
{
    Console.Write(res);
}
*/

// Try #3
/*
var results = await api.Chat.CreateChatCompletionAsync(new ChatRequest()
{
    Model = Model.ChatGPTTurbo,
    Temperature = 0.1,
    MaxTokens = 50,
    Messages = new ChatMessage[] {
            new ChatMessage(ChatMessageRole.User, "Who am I!")
        }
});

var reply = results.Choices[0].Message;
Console.WriteLine($"{reply.Role}: {reply.Content.Trim()}");
*/

// Try 4
await foreach (var token in api.Completions.StreamCompletionEnumerableAsync(
    new CompletionRequest("My name is Alexander and I am a lead software engineer at EPAM. This is my resume:", Model.DavinciText, 200, 0.5, presencePenalty: 0.1, frequencyPenalty: 0.1)))
{
    Console.Write(token);
}
