using OpenAI.GPT3.Managers;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3;

var secretAppsettingReader = new SecretAppsettingReader.SecretAppsettingReader();
var chatGptApiKey = secretAppsettingReader.ReadSection<string>("ChatGPTApiKey", true, typeof(Program).Assembly);

var gpt3 = new OpenAIService(new OpenAiOptions()
{
    ApiKey = chatGptApiKey!
});


var completionResult = await gpt3.Completions.CreateCompletion(new CompletionCreateRequest()
{
    Prompt = "What is the meaning of life?",
    // Model = Models.ChatGpt3_5Turbo - This is a chat model and not supported in the v1/completions endpoint. Did you mean to use v1/chat/completions?
    Model = Models.TextDavinciV2, // .TextDavinciV3,
    Temperature = 0.5F,
    MaxTokens = 100,
    N = 4
});

if (completionResult.Successful)
{
    foreach (var choice in completionResult.Choices)
    {
        Console.WriteLine(choice.Text);
    }
}
else
{
    if (completionResult.Error == null)
    {
        throw new Exception("Unknown Error");
    }
    Console.WriteLine($"{completionResult.Error.Code}: {completionResult.Error.Message}");
}

Console.ReadLine();
