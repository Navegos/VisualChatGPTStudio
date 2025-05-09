using Newtonsoft.Json;
using OpenAI_API.Functions;
using System.Collections.Generic;
using System.Linq;

namespace OpenAI_API.Chat
{
    /// <summary>
    /// A request to the Chat API. This is similar, but not exactly the same as the <see cref="Completions.CompletionRequest"/>
    /// Based on the <see href="https://platform.openai.com/docs/api-reference/chat">OpenAI API docs</see>
    /// </summary>
    public class ChatRequest
    {
        /// <summary>
        /// The model to use for this request
        /// </summary>
        [JsonProperty("model")]
        public string Model { get; set; } = OpenAI_API.Models.Model.ChatGPTTurbo;

        /// <summary>
        /// The messages to send with this Chat Request
        /// </summary>
        [JsonProperty("messages")]
        public IList<ChatMessage> Messages { get; set; }

        /// <summary>
        /// What sampling temperature to use. Higher values means the model will take more risks. Try 0.9 for more creative applications, and 0 (argmax sampling) for ones with a well-defined answer. It is generally recommend to use this or <see cref="TopP"/> but not both.
        /// </summary>
        [JsonProperty("temperature")]
        public double? Temperature { get; set; }

        /// <summary>
        /// An alternative to sampling with temperature, called nucleus sampling, where the model considers the results of the tokens with top_p probability mass. So 0.1 means only the tokens comprising the top 10% probability mass are considered. It is generally recommend to use this or <see cref="Temperature"/> but not both.
        /// </summary>
        [JsonProperty("top_p")]
        public double? TopP { get; set; }

        /// <summary>
        /// How many different choices to request for each message. Defaults to 1.
        /// </summary>
        [JsonProperty("n")]
        public int? NumChoicesPerMessage { get; set; }

        /// <summary>
        /// Specifies where the results should stream and be returned at one time.  Do not set this yourself, use the appropriate methods on <see cref="OpenAI_API.Completions.CompletionEndpoint"/> instead.
        /// </summary>
        [JsonProperty("stream")]
        public bool Stream { get; internal set; } = false;

        /// <summary>
        /// This is only used for serializing the request into JSON, do not use it directly.
        /// </summary>
        [JsonProperty("stop")]
        internal object CompiledStop
        {
            get
            {
                if (MultipleStopSequences?.Length == 1)
                {
                    return StopSequence;
                }
                else if (MultipleStopSequences?.Length > 0)
                {
                    return MultipleStopSequences;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// One or more sequences where the API will stop generating further tokens. The returned text will not contain the stop sequence.
        /// </summary>
        [JsonIgnore]
        public string[] MultipleStopSequences { get; set; }

        /// <summary>
        /// The stop sequence where the API will stop generating further tokens. The returned text will not contain the stop sequence.  For convenience, if you are only requesting a single stop sequence, set it here
        /// </summary>
        [JsonIgnore]
        public string StopSequence
        {
            get => MultipleStopSequences?.FirstOrDefault() ?? null;
            set
            {
                if (value != null)
                {
                    MultipleStopSequences = new string[] { value };
                }
            }
        }

        /// <summary>
        /// How many tokens to complete to. Can return fewer if a stop sequence is hit.  Defaults to 16.
        /// </summary>
        [JsonProperty("max_tokens")]
        public int? MaxTokens { get; set; }

        /// <summary>
        /// The scale of the penalty for how often a token is used.  Should generally be between 0 and 1, although negative numbers are allowed to encourage token reuse.  Defaults to 0.
        /// </summary>
        [JsonProperty("frequency_penalty")]
        public double? FrequencyPenalty { get; set; }


        /// <summary>
        /// The scale of the penalty applied if a token is already present at all.  Should generally be between 0 and 1, although negative numbers are allowed to encourage token reuse.  Defaults to 0.
        /// </summary>
        [JsonProperty("presence_penalty")]
        public double? PresencePenalty { get; set; }

        /// <summary>
        /// Modify the likelihood of specified tokens appearing in the completion.
        /// Accepts a json object that maps tokens(specified by their token ID in the tokenizer) to an associated bias value from -100 to 100.
        /// Mathematically, the bias is added to the logits generated by the model prior to sampling.
        /// The exact effect will vary per model, but values between -1 and 1 should decrease or increase likelihood of selection; values like -100 or 100 should result in a ban or exclusive selection of the relevant token.
        /// </summary>
        [JsonProperty("logit_bias")]
        public IReadOnlyDictionary<string, float> LogitBias { get; set; }

        /// <summary>
        /// A unique identifier representing your end-user, which can help OpenAI to monitor and detect abuse.
        /// </summary>
        [JsonProperty("user")]
        public string user { get; set; }

        /// <summary>
        /// Get or set the tools for function requests
        /// </summary>
        [JsonProperty("tools")]
        public List<FunctionRequest> Tools { get; set; }

        /// <summary>
        /// Creates a new, empty <see cref="ChatRequest"/>
        /// </summary>
        public ChatRequest()
        { }

        /// <summary>
        /// Create a new chat request using the data from the input chat request.
        /// </summary>
        /// <param name="basedOn"></param>
        public ChatRequest(ChatRequest basedOn)
        {
            if (basedOn == null)
            {
                return;
            }

            this.Model = basedOn.Model;
            this.Messages = basedOn.Messages;
            this.Temperature = basedOn.Temperature;
            this.TopP = basedOn.TopP;
            this.NumChoicesPerMessage = basedOn.NumChoicesPerMessage;
            this.MultipleStopSequences = basedOn.MultipleStopSequences;
            this.MaxTokens = basedOn.MaxTokens;
            this.FrequencyPenalty = basedOn.FrequencyPenalty;
            this.PresencePenalty = basedOn.PresencePenalty;
            this.LogitBias = basedOn.LogitBias;
        }
    }
}
