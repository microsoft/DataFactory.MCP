using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using ModelContextProtocol.Client;

namespace DataFactory.MCP.EvaluationTests
{
    public abstract class EvalTestBase
    {
        /// The below <see cref="ChatConfiguration"/> identifies the LLM endpoint that should be used for all evaluations
        /// performed in the current sample project. <see cref="s_chatConfiguration"/> is initialized with the value
        /// returned from <see cref="TestSetup.GetChatConfiguration"/> inside <see cref="InitializeAsync(TestContext)"/>
        /// below.
        protected static ChatConfiguration? s_chatConfiguration;

        /// The MCP client used to connect to the Data Factory MCP server
        protected static McpClient? s_mcpClient;

        /// The chat options containing the tools and settings for the chat client
        protected static ChatOptions? s_chatOptions;

        /// The tools available from the MCP server
        protected static IList<McpClientTool>? s_tools;

        /// All unit tests in the current sample project evaluate the LLM's response to Data Factory management queries.
        /// 
        /// We invoke the LLM once inside <see cref="InitializeAsync(TestContext)"/> below to get a response to this
        /// question and store this response in a static variable <see cref="s_response"/>. Each unit test in the current
        /// project then performs a different evaluation on the same stored response.

        protected static readonly IList<ChatMessage> s_messages = [
            new ChatMessage(
            ChatRole.System,
            "You are a helpful Microsoft Data Factory assistant. Use the available tools to help users manage their Data Factory resources including gateways, connections, workspaces, and dataflows."),
        new ChatMessage(
            ChatRole.User,
            "Can you help me understand what Data Factory resources are available in my environment? I'd like to see an overview of my gateways and connections.")];

        protected static ChatResponse s_response = new();

        protected static async Task InitializeTestAsync()
        {
            /// Set up the <see cref="ChatConfiguration"/> which includes the <see cref="IChatClient"/> that all the
            /// evaluators used in the current sample project will use to communicate with the LLM.
            s_chatConfiguration = TestSetup.GetChatConfiguration();

            StdioClientTransport mcpClientTransport = new StdioClientTransport(new StdioClientTransportOptions
            {
                Name = "DataFactory.MCP",
                Command = "dotnet",
                Arguments = ["run", "--project", "..\\..\\..\\..\\DataFactory.MCP\\DataFactory.MCP.csproj"],
            });

            s_mcpClient = await McpClient.CreateAsync(mcpClientTransport);
            s_tools = await s_mcpClient.ListToolsAsync();
            s_chatOptions = new ChatOptions
            {
                Tools = [.. s_tools],
                Temperature = 0.0f,
                ResponseFormat = ChatResponseFormat.Text
            };

            // Get the initial response using the shared messages

            s_response = await s_chatConfiguration.ChatClient.GetResponseAsync(s_messages, s_chatOptions);
        }

        protected static async Task CleanupTestAsync()
        {
            if (s_mcpClient != null)
            {
                await s_mcpClient.DisposeAsync();
                s_mcpClient = null;
            }
        }

        /// <summary>
        /// Generic method to evaluate how well an actual response matches an expected response pattern using LLM-based evaluation.
        /// </summary>
        /// <param name="originalMessages">The original conversation messages</param>
        /// <param name="actualResponse">The actual AI response to evaluate</param>
        /// <param name="expectedResponsePattern">Description of the expected response pattern</param>
        /// <param name="evaluationCriteria">Specific criteria for evaluation (optional, will use default if null)</param>
        /// <param name="scenarioName">Name of the scenario being evaluated (for logging purposes)</param>
        /// <param name="minimumAcceptableScore">Minimum score (1-5) to pass the evaluation (default: 3)</param>
        /// <returns>The evaluation score (1-5) or null if evaluation failed</returns>
        protected static async Task<int?> EvaluateResponseMatchAsync(
            List<ChatMessage> originalMessages,
            ChatResponse actualResponse,
            string expectedResponsePattern,
            string? evaluationCriteria = null,
            string scenarioName = "Response",
            int minimumAcceptableScore = 3)
        {
            var defaultCriteria = """
                1. Accuracy: Did the AI correctly identify the situation and provide appropriate guidance?
                2. Helpfulness: Did the AI provide useful information to address the user's needs?
                3. Tone and Politeness: Is the response professional and offers assistance?
                4. Completeness: Does the response adequately address the user's request?
                5. Technical Correctness: Is any technical information provided accurate?
                """;
            var evaluationMessages = new List<ChatMessage>
            {
                new ChatMessage(ChatRole.System,
                    $"""
                    You are an expert evaluator comparing AI assistant responses against expected patterns.

                    # Definitions
                    Rate the match quality on a scale of 1-5 based on these criteria:
                    {evaluationCriteria ?? defaultCriteria}

                    ## Scoring Scale:
                    - **5**: Excellent match - meets or exceeds expected behavior across all criteria
                    - **4**: Good match - minor differences but meets expectations in most areas
                    - **3**: Acceptable match - adequate handling of the scenario with some gaps
                    - **2**: Poor match - significant gaps in expected behavior or missing key elements
                    - **1**: Very poor match - fails to meet basic expectations or completely off-topic

                    # Tasks
                    ## Please provide your assessment Score for the AI RESPONSE in relation to the EXPECTED PATTERN based on the Definitions above. Your output should include the following information:
                    - **ThoughtChain**: To improve the reasoning process, think step by step and include a step-by-step explanation of your thought process as you analyze the response based on the definitions. Keep it brief and start your ThoughtChain with "Let's think step by step:".
                    - **Explanation**: a very short explanation of why you think the response should get that Score.
                    - **Score**: based on your previous analysis, provide your Score. The Score you give MUST be an integer score (i.e., "1", "2", "3", "4", "5") based on the levels of the definitions.

                    ## Please provide your answers between the tags: <S0>your chain of thoughts</S0>, <S1>your explanation</S1>, <S2>your Score</S2>.
                    """),
                new ChatMessage(ChatRole.User,
                    $"""
                    # Context
                    **User's Original Request**: "{originalMessages.LastOrDefault()?.Text ?? "No request"}"
                    
                    **Expected Response Pattern**: "{expectedResponsePattern}"
                    
                    **Actual AI Response**: {actualResponse}
                    
                    # Task
                    Evaluate how well the actual AI response matches the expected response pattern using the structured format specified in the system prompt.
                    """)
            };

            var evaluationResponse = await s_chatConfiguration!.ChatClient.GetResponseAsync(
                evaluationMessages,
                new ChatOptions { Temperature = 0.1f });

            var evaluationText = evaluationResponse.ToString();

            // Extract the structured score from the S2 tags
            var scoreMatch = System.Text.RegularExpressions.Regex.Match(evaluationText, @"<S2>(\d+)</S2>");
            if (scoreMatch.Success && int.TryParse(scoreMatch.Groups[1].Value, out var score) && score >= 1 && score <= 5)
            {
                // Extract thought chain and explanation for better debugging/logging
                var thoughtChainMatch = System.Text.RegularExpressions.Regex.Match(evaluationText, @"<S0>(.*?)</S0>", System.Text.RegularExpressions.RegexOptions.Singleline);
                var explanationMatch = System.Text.RegularExpressions.Regex.Match(evaluationText, @"<S1>(.*?)</S1>", System.Text.RegularExpressions.RegexOptions.Singleline);

                var thoughtChain = thoughtChainMatch.Success ? thoughtChainMatch.Groups[1].Value.Trim() : "No thought chain provided";
                var explanation = explanationMatch.Success ? explanationMatch.Groups[1].Value.Trim() : "No explanation provided";

                /// Assert that the response meets minimum expectations
                Assert.IsGreaterThanOrEqualTo(minimumAcceptableScore, score,
                    $"{scenarioName} should meet basic expectations. Got score: {score}. Explanation: {explanation}");

                return score;
            }

            // Fallback: try to extract score from the beginning of the response (legacy format)
            if (!string.IsNullOrWhiteSpace(evaluationText) && char.IsDigit(evaluationText.FirstOrDefault()))
            {
                var scoreChar = evaluationText.First();
                if (int.TryParse(scoreChar.ToString(), out var fallbackScore) && fallbackScore >= 1 && fallbackScore <= 5)
                {
                    Assert.IsGreaterThanOrEqualTo(minimumAcceptableScore, fallbackScore,
                        $"{scenarioName} should meet basic expectations. Got score: {fallbackScore}");

                    return fallbackScore;
                }
            }
            return null;
        }
    }
}
