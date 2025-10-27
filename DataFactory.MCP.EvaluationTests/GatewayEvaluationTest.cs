using Microsoft.Extensions.AI;

namespace DataFactory.MCP.EvaluationTests;

[TestClass]
public sealed class GatewayEvaluationTest : EvalTestBase
{
    [ClassInitialize]
    public static async Task InitializeAsync(TestContext testContext)
    {
        await InitializeTestAsync();
    }

    [ClassCleanup]
    public static async Task CleanupAsync()
    {
        await CleanupTestAsync();
    }

    [TestMethod]
    public async Task TestGatewayAuthenticationHandling()
    {
        /// This test evaluates how well the AI agent handles authentication requirements
        /// when users request gateway management operations without being authenticated.
        /// 
        /// The test compares the actual response against an expected response pattern
        /// and evaluates the quality of the authentication guidance provided.

        IChatClient chatClient = s_chatConfiguration!.ChatClient;

        /// Create a conversation about gateway management (user not authenticated)
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.System,
                "You are a helpful Microsoft Data Factory assistant. " +
                "Use the available tools to help users manage their Data Factory gateways. " +
                "Provide accurate information about gateway types, status, and configurations. " +
                "If authentication is required, inform the user about the authentication process."),
            new ChatMessage(ChatRole.User,
                "I need help with my Data Factory setup. Can you show me all the gateways available in my environment? " +
                "I'm particularly interested in understanding what types of gateways I have and their current status.")
        };

        /// Define the expected response pattern for authentication scenarios
        var expectedAuthResponse =
            "The AI should recognize that authentication is required and provide helpful guidance " +
            "about how to authenticate with Azure AD or other authentication methods. " +
            "The response should be polite, informative, and offer to help with the authentication process.";

        /// Get the AI response using the shared chat options from base class
        ChatResponse actualResponse = await chatClient.GetResponseAsync(messages, s_chatOptions!);

        /// Define evaluation criteria specific to authentication scenarios
        var authenticationCriteria = """
            1. Authentication Recognition: Did the AI correctly identify that authentication is needed?
            2. User Guidance: Did the AI provide helpful information about authentication options (e.g., Azure AD)?
            3. Tone and Helpfulness: Is the response polite and offers assistance?
            4. Technical Accuracy: Is the authentication guidance technically correct?
            5. Completeness: Does the response address the user's request appropriately?
            """;

        /// Evaluate the response against expected authentication handling behavior using the generic method
        var score = await EvaluateResponseMatchAsync(
            messages,
            actualResponse,
            expectedAuthResponse,
            authenticationCriteria,
            "Authentication Handling",
            minimumAcceptableScore: 3);
    }
}