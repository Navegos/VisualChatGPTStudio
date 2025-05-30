﻿using OpenAI_API.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace OpenAI_API.Chat
{
    /// <summary>
    /// Represents on ongoing chat with back-and-forth interactions between the user and the chatbot.  This is the simplest way to interact with the ChatGPT API, rather than manually using the ChatEnpoint methods.  You do lose some flexibility though.
    /// </summary>
    public class Conversation
    {
        /// <summary>
        /// An internal reference to the API endpoint, needed for API requests
        /// </summary>
        private readonly ChatEndpoint endpoint;

        /// <summary>
        /// The tools object used to make function execution requests.
        /// </summary>
        private readonly List<FunctionRequest> tools;

        /// <summary>
        /// Allows setting the parameters to use when calling the ChatGPT API.  Can be useful for setting temperature, presence_penalty, and more.  <see href="https://platform.openai.com/docs/api-reference/chat/create">Se  OpenAI documentation for a list of possible parameters to tweak.</see>
        /// </summary>
        public ChatRequest RequestParameters { get; private set; }

        /// <summary>
        /// Specifies the model to use for ChatGPT requests.  This is just a shorthand to access <see cref="RequestParameters"/>.Model
        /// </summary>
        public OpenAI_API.Models.Model Model
        {
            get
            {
                return RequestParameters.Model;
            }
            set
            {
                RequestParameters.Model = value;
            }
        }

        /// <summary>
        /// After calling <see cref="GetResponseFromChatbotAsync"/>, this contains the full response object which can contain useful metadata like token usages, <see cref="ChatChoice.FinishReason"/>, etc.  This is overwritten with every call to <see cref="GetResponseFromChatbotAsync"/> and only contains the most recent result.
        /// </summary>
        public ChatResult MostRecentApiResult { get; private set; }

        /// <summary>
        /// Creates a new conversation with ChatGPT chat
        /// </summary>
        /// <param name="endpoint">A reference to the API endpoint, needed for API requests.  Generally should be <see cref="OpenAIAPI.Chat"/>.</param>
        /// <param name="model">Optionally specify the model to use for ChatGPT requests.  If not specified, used <paramref name="defaultChatRequestArgs"/>.Model or falls back to <see cref="OpenAI_API.Models.Model.DefaultChatModel"/></param>
        /// <param name="defaultChatRequestArgs">Allows setting the parameters to use when calling the ChatGPT API.  Can be useful for setting temperature, presence_penalty, and more.  See <see href="https://platform.openai.com/docs/api-reference/chat/create">OpenAI documentation for a list of possible parameters to tweak.</see></param>
        public Conversation(ChatEndpoint endpoint, OpenAI_API.Models.Model model = null, ChatRequest defaultChatRequestArgs = null)
        {
            RequestParameters = new ChatRequest(defaultChatRequestArgs);
            if (model != null)
            {
                RequestParameters.Model = model;
            }

            if (RequestParameters.Model == null)
            {
                RequestParameters.Model = Models.Model.DefaultChatModel;
            }

            _Messages = new List<ChatMessage>();
            tools = new List<FunctionRequest>();
            this.endpoint = endpoint;
            RequestParameters.NumChoicesPerMessage = 1;
            RequestParameters.Stream = false;
        }

        /// <summary>
        /// A list of messages exchanged so far. To append to this list, use <see cref="AppendMessage(ChatMessage)"/>, <see cref="AppendUserInput(string)"/>, <see cref="AppendSystemMessage(string)"/>, or <see cref="AppendExampleChatbotOutput(string)"/>.
        /// </summary>
        public IList<ChatMessage> Messages { get => _Messages; }
        private readonly List<ChatMessage> _Messages;

        /// <summary>
        /// Appends a <see cref="ChatMessage"/> to the chat history
        /// </summary>
        /// <param name="message">The <see cref="ChatMessage"/> to append to the chat history</param>
        public void AppendMessage(ChatMessage message)
        {
            _Messages.Add(message);
        }

        /// <summary>
        /// Creates and appends a <see cref="ChatMessage"/> to the chat history
        /// </summary>
        /// <param name="role">The <see cref="ChatMessageRole"/> for the message.  Typically, a conversation is formatted with a system message first, followed by alternating user and assistant messages.  See <see href="https://platform.openai.com/docs/guides/chat/introduction">the OpenAI docs</see> for more details about usage.</param>
        /// <param name="content">The content of the message)</param>
        public void AppendMessage(ChatMessageRole role, string content) => this.AppendMessage(new ChatMessage(role, content));

        /// <summary>
        /// Creates and appends a <see cref="ChatMessage"/> to the chat history with the Role of <see cref="ChatMessageRole.User"/>.  The user messages help instruct the assistant. They can be generated by the end users of an application, or set by a developer as an instruction.
        /// </summary>
        /// <param name="content">The content of the user input to be appended.</param>
        public void AppendUserInput(object content) => this.AppendMessage(new ChatMessage(ChatMessageRole.User, content));

        /// <summary>
        /// Creates and appends a <see cref="ChatMessage"/> to the chat history with the Role of <see cref="ChatMessageRole.User"/>.  The user messages help instruct the assistant. They can be generated by the end users of an application, or set by a developer as an instruction.
        /// </summary>
        /// <param name="userName">The name of the user in a multi-user chat</param>
        /// <param name="content">Text content generated by the end users of an application, or set by a developer as an instruction</param>
        public void AppendUserInputWithName(string userName, string content) => this.AppendMessage(new ChatMessage(ChatMessageRole.User, content) { Name = userName });

        /// <summary>
        /// Creates and appends a <see cref="ChatMessage"/> to the chat history with the Role of <see cref="ChatMessageRole.System"/>.  The system message helps set the behavior of the assistant.
        /// </summary>
        /// <param name="content">text content that helps set the behavior of the assistant</param>
        public void AppendSystemMessage(string content) => this.AppendMessage(new ChatMessage(ChatMessageRole.System, content));

        /// <summary>
        /// Creates and appends a <see cref="ChatMessage"/> to the chat history with the Role of <see cref="ChatMessageRole.Assistant"/>.  Assistant messages can be written by a developer to help give examples of desired behavior.
        /// </summary>
        /// <param name="content">Text content written by a developer to help give examples of desired behavior</param>
        public void AppendExampleChatbotOutput(string content) => this.AppendMessage(new ChatMessage(ChatMessageRole.Assistant, content));

        /// <summary>
        /// Appends a function request to the list of tools.
        /// </summary>
        /// <param name="functionRequest">The function request to be added.</param>
        public void AppendFunctionCall(FunctionRequest functionRequest)
        {
            tools.Add(functionRequest);
        }

        /// <summary>
        /// Appends a tool message to the chat by creating a new ChatMessage with the specified role, content, and function ID.
        /// </summary>
        public void AppendToolMessage(string functionId, string content) => this.AppendMessage(new ChatMessage(ChatMessageRole.Tool, content, functionId));

        /// <summary>
        /// An event called when the chat message history is too long, which should reduce message history length through whatever means is appropriate for your use case.  You may want to remove the first entry in the <see cref="List{ChatMessage}"/> in the <see cref="EventArgs"/>
        /// </summary>
        public event EventHandler<List<ChatMessage>> OnTruncationNeeded;

        /// <summary>
        /// Sometimes the total length of your conversation can get too long to fit in the ChatGPT context window.  In this case, the <see cref="OnTruncationNeeded"/> event will be called, if supplied.  If not supplied and this is <see langword="true"/>, then the first one or more user or assistant messages will be automatically deleted from the beginning of the conversation message history until the API call succeeds.  This may take some time as it may need to loop several times to clear enough messages.  If this is set to false and no <see cref="OnTruncationNeeded"/> is supplied, then an <see cref="ArgumentOutOfRangeException"/> will be raised when the API returns a context_length_exceeded error.
        /// </summary>
        public bool AutoTruncateOnContextLengthExceeded { get; set; } = true;

        #region Non-streaming

        /// <summary>
        /// Asynchronously retrieves the content of AI response.
        /// </summary>
        /// <returns>
        /// A string representing the content of the AI response, or null if the response or content is unavailable.
        /// </returns>
        public async Task<string> GetResponseContentAsync()
        {
            ChatMessage response = await GetResponseFromChatbotAsync();

            return response?.Content?.ToString();
        }

        /// <summary>
        /// Asynchronously retrieves the response content and associated functions from a chatbot response.
        /// </summary>
        /// <returns>
        /// A tuple containing the response content as a string and a list of function results.
        /// </returns>
        public async Task<(string, List<FunctionResult>)> GetResponseContentAndFunctionAsync()
        {
            ChatMessage response = await GetResponseFromChatbotAsync();

            return (response?.Content?.ToString(), response?.Functions?.ToList());
        }

        /// <summary>
        /// Sends a request to the chatbot endpoint with the current set of messages and request parameters, and returns the response message content.
        /// </summary>
        /// <returns>
        /// The content of the response message from the chatbot endpoint, or null if an error occurred or no response was received.
        /// </returns>
        private async Task<ChatMessage> GetResponseFromChatbotAsync()
        {
            try
            {
                ChatRequest req = new ChatRequest(RequestParameters)
                {
                    Messages = Messages.ToList(),
                    Tools = tools.Any() ? tools : null
                };

                ChatResult res = await endpoint.CreateChatCompletionAsync(req);

                MostRecentApiResult = res;

                if (res.Choices.Count > 0)
                {
                    ChatMessage newMsg = res.Choices[0].Message;

                    AppendMessage(newMsg);

                    return newMsg;
                }
            }
            catch (HttpRequestException ex)
            {
                if (TruncateContextWhenExceeded(ex))
                {
                    return await GetResponseFromChatbotAsync();
                }
            }

            return null;
        }

        #endregion

        #region Streaming

        /// <summary>
        /// Calls the API to get a response, which is appended to the current chat's <see cref="Messages"/> as an <see cref="ChatMessageRole.Assistant"/> <see cref="ChatMessage"/>, and streams the results to the <paramref name="resultHandler"/> as they come in. <br/>
        /// If you are on the latest C# supporting async enumerables, you may prefer the cleaner syntax of <see cref="StreamResponseEnumerableFromChatbotAsync"/> instead.
        ///  </summary>
        /// <param name="resultHandler">An action to be called as each new result arrives.</param>
        public async Task StreamResponseFromChatbotAsync(Action<string> resultHandler)
        {
            await foreach (string res in StreamResponseEnumerableFromChatbotAsync())
            {
                resultHandler(res);
            }
        }

        /// <summary>
        /// Calls the API to get a response, which is appended to the current chat's <see cref="Messages"/> as an <see cref="ChatMessageRole.Assistant"/> <see cref="ChatMessage"/>, and streams the results to the <paramref name="resultHandler"/> as they come in. <br/>
        /// If you are on the latest C# supporting async enumerables, you may prefer the cleaner syntax of <see cref="StreamResponseEnumerableFromChatbotAsync"/> instead.
        ///  </summary>
        /// <param name="resultHandler">An action to be called as each new result arrives, which includes the index of the result in the overall result set.</param>
        public async Task StreamResponseFromChatbotAsync(Action<int, string> resultHandler)
        {
            int index = 0;
            await foreach (string res in StreamResponseEnumerableFromChatbotAsync())
            {
                resultHandler(index++, res);
            }
        }

        /// <summary>
        /// Calls the API to get a response, which is appended to the current chat's <see cref="Messages"/> as an <see cref="ChatMessageRole.Assistant"/> <see cref="ChatMessage"/>, and streams the results as they come in. <br/>
        /// If you are not using C# 8 supporting async enumerables or if you are using the .NET Framework, you may need to use <see cref="StreamResponseFromChatbotAsync(Action{string})"/> instead.
        /// </summary>
        /// <returns>An async enumerable with each of the results as they come in.  See <see href="https://docs.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-8#asynchronous-streams"/> for more details on how to consume an async enumerable.</returns>
        public async IAsyncEnumerable<string> StreamResponseEnumerableFromChatbotAsync()
        {
            ChatRequest request;

            StringBuilder responseStringBuilder = new StringBuilder();
            ChatMessageRole responseRole = null;

            IAsyncEnumerable<ChatResult> resStream = null;

            bool retrying = true;
            bool streamError = false;
            ChatResult firstStreamedResult;
            IAsyncEnumerator<ChatResult> enumerator = null;

            while (retrying)
            {
                retrying = false;
                request = new ChatRequest(RequestParameters) { Messages = Messages.ToList() };

                try
                {
                    resStream = endpoint.StreamChatEnumerableAsync(request);
                    enumerator = resStream.GetAsyncEnumerator();
                    await enumerator.MoveNextAsync();
                    firstStreamedResult = enumerator.Current;
                }
                catch (HttpRequestException ex)
                {
                    retrying = TruncateContextWhenExceeded(ex);
                }
                catch (ArgumentException)
                {
                    streamError = true;
                }
            }

            //In case of an error reading the stream, it returns a single response.
            if (streamError)
            {
                ChatMessage result = await GetResponseFromChatbotAsync();

                yield return result?.Content?.ToString();

                yield break;
            }

            if (resStream == null)
            {
                throw new Exception("The chat result stream is null, but it shouldn't be");
            }

            do
            {
                ChatResult res = enumerator.Current;

                if (res.Choices.FirstOrDefault()?.Delta is ChatMessage delta)
                {
                    if (delta.Role != null)
                    {
                        responseRole = delta.Role;
                    }

                    string deltaTextContent = delta.Content?.ToString();

                    if (!string.IsNullOrWhiteSpace(deltaTextContent))
                    {
                        responseStringBuilder.Append(deltaTextContent);

                        yield return deltaTextContent;
                    }
                }

                MostRecentApiResult = res;

            } while (await enumerator.MoveNextAsync());

            if (responseRole != null)
            {
                AppendMessage(responseRole, responseStringBuilder.ToString());
            }
        }

        #endregion

        /// <summary>
        /// Truncates the context of the chat messages when the HttpRequestException contains the "context_length_exceeded" code.
        /// </summary>
        /// <param name="ex">The HttpRequestException that was thrown.</param>
        /// <returns>True if the context was truncated, false otherwise.</returns>
        private bool TruncateContextWhenExceeded(HttpRequestException ex)
        {
            if (!ex.Data.Contains("code") || string.IsNullOrWhiteSpace(ex.Data["code"]?.ToString()) || !ex.Data["code"].Equals("context_length_exceeded"))
            {
                throw ex;
            }

            for (int i = 0; i < Messages.Count; i++)
            {
                if (Messages[i].Role != ChatMessageRole.System)
                {
                    Messages.RemoveAt(i);

                    return true;
                }
            }

            return false;
        }
    }
}
