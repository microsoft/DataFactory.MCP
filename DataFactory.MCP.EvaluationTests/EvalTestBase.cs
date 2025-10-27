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
        /// Now internally uses the ResponseMatchEvaluator (IEvaluator) for consistency while maintaining backward compatibility.
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
            // Use the IEvaluator internally for consistency
            var evaluator = new ResponseMatchEvaluator();
            var context = new ResponseMatchEvaluatorContext(
                expectedResponsePattern,
                evaluationCriteria,
                scenarioName);

            var result = await evaluator.EvaluateAsync(
                originalMessages,
                actualResponse,
                s_chatConfiguration,
                [context]);

            // Extract the score and validate it meets minimum expectations
            var matchScoreMetric = result.Metrics.OfType<NumericMetric>()
                .FirstOrDefault(m => m.Name == ResponseMatchEvaluator.MatchScoreMetricName);

            if (matchScoreMetric?.Value is double score)
            {
                var intScore = (int)score;
                Assert.IsGreaterThanOrEqualTo(minimumAcceptableScore, intScore,
                    $"{scenarioName} should meet basic expectations. Got score: {intScore}. Explanation: {matchScoreMetric.Reason}");

                return intScore;
            }

            return null;
        }

        /// <summary>
        /// Enhanced method that uses the IEvaluator pattern with ResponseMatchEvaluator.
        /// Returns a proper EvaluationResult with structured metrics and diagnostics.
        /// This is the recommended method for new code that needs access to detailed evaluation results.
        /// </summary>
        /// <param name="originalMessages">The original conversation messages</param>
        /// <param name="actualResponse">The actual AI response to evaluate</param>
        /// <param name="expectedResponsePattern">Description of the expected response pattern</param>
        /// <param name="evaluationCriteria">Specific criteria for evaluation (optional, will use default if null)</param>
        /// <param name="scenarioName">Name of the scenario being evaluated (for logging purposes)</param>
        /// <param name="minimumAcceptableScore">Minimum score (1-5) to pass the evaluation (default: 3)</param>
        /// <returns>The evaluation result containing the match score and metrics</returns>
        protected static async Task<EvaluationResult> EvaluateWithIEvaluatorAsync(
            List<ChatMessage> originalMessages,
            ChatResponse actualResponse,
            string expectedResponsePattern,
            string? evaluationCriteria = null,
            string scenarioName = "Response",
            int minimumAcceptableScore = 3)
        {
            var evaluator = new ResponseMatchEvaluator();
            var context = new ResponseMatchEvaluatorContext(
                expectedResponsePattern,
                evaluationCriteria,
                scenarioName);

            var result = await evaluator.EvaluateAsync(
                originalMessages,
                actualResponse,
                s_chatConfiguration,
                [context]);

            // Validate the result meets minimum expectations
            var matchScoreMetric = result.Metrics.OfType<NumericMetric>()
                .FirstOrDefault(m => m.Name == ResponseMatchEvaluator.MatchScoreMetricName);

            if (matchScoreMetric?.Value is double score)
            {
                Assert.IsGreaterThanOrEqualTo(minimumAcceptableScore, score,
                    $"{scenarioName} should meet basic expectations. Got score: {score}. Explanation: {matchScoreMetric.Reason}");
            }

            return result;
        }

        /// <summary>
        /// Helper method to extract the numeric score from a ResponseMatchEvaluator result.
        /// </summary>
        /// <param name="evaluationResult">The evaluation result from ResponseMatchEvaluator</param>
        /// <returns>The numeric score (1-5) or null if not found</returns>
        protected static int? GetMatchScore(EvaluationResult evaluationResult)
        {
            var matchScoreMetric = evaluationResult.Metrics.OfType<NumericMetric>()
                .FirstOrDefault(m => m.Name == ResponseMatchEvaluator.MatchScoreMetricName);

            return matchScoreMetric?.Value is double score ? (int)score : null;
        }
    }
}