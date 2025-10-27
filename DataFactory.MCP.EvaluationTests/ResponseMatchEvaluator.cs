// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;

namespace DataFactory.MCP.EvaluationTests;

/// <summary>
/// An <see cref="IEvaluator"/> that evaluates how well an actual response matches an expected response pattern using LLM-based evaluation.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ResponseMatchEvaluator"/> measures the degree to which the response being evaluated matches the expected response pattern.
/// It returns a <see cref="NumericMetric"/> that contains a score for the 'Match Quality'. The score is a number between 1 and 5,
/// with 1 indicating a poor match, and 5 indicating an excellent match.
/// </para>
/// <para>
/// <b>Note:</b> <see cref="ResponseMatchEvaluator"/> is an AI-based evaluator that uses an AI model to perform its
/// evaluation. While the prompt that this evaluator uses to perform its evaluation is designed to be model-agnostic,
/// the performance of this prompt (and the resulting evaluation) can vary depending on the model used, and can be
/// especially poor when a smaller / local model is used.
/// </para>
/// </remarks>
public sealed class ResponseMatchEvaluator : IEvaluator
{
    /// <summary>
    /// Gets the <see cref="EvaluationMetric.Name"/> of the <see cref="NumericMetric"/> returned by
    /// <see cref="ResponseMatchEvaluator"/>.
    /// </summary>
    public static string MatchScoreMetricName => "MatchScore";

    /// <inheritdoc/>
    public IReadOnlyCollection<string> EvaluationMetricNames { get; } = [MatchScoreMetricName];

    private static readonly ChatOptions _chatOptions = new ChatOptions
    {
        Temperature = 0.1f,
        MaxOutputTokens = 800,
        TopP = 1.0f,
        PresencePenalty = 0.0f,
        FrequencyPenalty = 0.0f,
        ResponseFormat = ChatResponseFormat.Text
    };

    /// <inheritdoc/>
    public async ValueTask<EvaluationResult> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(modelResponse);
        ArgumentNullException.ThrowIfNull(chatConfiguration);

        var metric = new NumericMetric(MatchScoreMetricName);
        var result = new EvaluationResult(metric);

        if (string.IsNullOrWhiteSpace(modelResponse.Text))
        {
            metric.AddDiagnostics(
                EvaluationDiagnostic.Error($"The {nameof(modelResponse)} supplied for evaluation was null or empty."));
            return result;
        }

        if (additionalContext?.OfType<ResponseMatchEvaluatorContext>().FirstOrDefault()
                is not ResponseMatchEvaluatorContext context)
        {
            metric.AddDiagnostics(
                EvaluationDiagnostic.Error(
                    $"A value of type {nameof(ResponseMatchEvaluatorContext)} was not found in the {nameof(additionalContext)} collection."));
            return result;
        }

        var messagesList = messages.ToList();
        var evaluationInstructions = GetEvaluationInstructions(messagesList, modelResponse, context);

        try
        {
            var evaluationResponse = await chatConfiguration.ChatClient.GetResponseAsync(
                evaluationInstructions,
                _chatOptions,
                cancellationToken).ConfigureAwait(false);

            if (TryParseEvaluationResponse(evaluationResponse, out var score, out var thoughtChain, out var explanation))
            {
                metric.Value = score;
                metric.Reason = explanation;
                
                if (!string.IsNullOrEmpty(thoughtChain))
                {
                    metric.AddDiagnostics(EvaluationDiagnostic.Informational(
                        $"Model's evaluation chain of thought: {thoughtChain}"));
                }
            }
            else
            {
                metric.AddDiagnostics(
                    EvaluationDiagnostic.Error(
                        $"Failed to parse score for '{metric.Name}' from the evaluation response: {evaluationResponse.Text}"));
            }
        }
        catch (Exception ex)
        {
            metric.AddDiagnostics(
                EvaluationDiagnostic.Error($"Error during evaluation: {ex.Message}"));
        }

        return result;
    }

    private static List<ChatMessage> GetEvaluationInstructions(
        List<ChatMessage> originalMessages,
        ChatResponse actualResponse,
        ResponseMatchEvaluatorContext context)
    {
        var defaultCriteria = """
            1. Accuracy: Did the AI correctly identify the situation and provide appropriate guidance?
            2. Helpfulness: Did the AI provide useful information to address the user's needs?
            3. Tone and Politeness: Is the response professional and offers assistance?
            4. Completeness: Does the response adequately address the user's request?
            5. Technical Correctness: Is any technical information provided accurate?
            """;

        var systemPrompt = $"""
            You are an expert evaluator comparing AI assistant responses against expected patterns.

            # Definitions
            Rate the match quality on a scale of 1-5 based on these criteria:
            {context.EvaluationCriteria ?? defaultCriteria}

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
            """;

        var userPrompt = $"""
            # Context
            **User's Original Request**: "{originalMessages.LastOrDefault()?.Text ?? "No request"}"
            
            **Expected Response Pattern**: "{context.ExpectedResponsePattern}"
            
            **Actual AI Response**: {actualResponse}
            
            # Task
            Evaluate how well the actual AI response matches the expected response pattern using the structured format specified in the system prompt.
            """;

        return [
            new ChatMessage(ChatRole.System, systemPrompt),
            new ChatMessage(ChatRole.User, userPrompt)
        ];
    }

    private static bool TryParseEvaluationResponse(
        ChatResponse evaluationResponse,
        out double score,
        out string? thoughtChain,
        out string? explanation)
    {
        score = 0;
        thoughtChain = null;
        explanation = null;

        var evaluationText = evaluationResponse.Text.Trim();

        // Extract the structured score from the S2 tags
        var scoreMatch = Regex.Match(evaluationText, @"<S2>(\d+)</S2>");
        if (scoreMatch.Success && int.TryParse(scoreMatch.Groups[1].Value, out var parsedScore) && parsedScore >= 1 && parsedScore <= 5)
        {
            score = parsedScore;

            // Extract thought chain and explanation for better debugging/logging
            var thoughtChainMatch = Regex.Match(evaluationText, @"<S0>(.*?)</S0>", RegexOptions.Singleline);
            var explanationMatch = Regex.Match(evaluationText, @"<S1>(.*?)</S1>", RegexOptions.Singleline);

            thoughtChain = thoughtChainMatch.Success ? thoughtChainMatch.Groups[1].Value.Trim() : null;
            explanation = explanationMatch.Success ? explanationMatch.Groups[1].Value.Trim() : null;

            return true;
        }

        // Fallback: try to extract score from the beginning of the response (legacy format)
        if (!string.IsNullOrWhiteSpace(evaluationText) && char.IsDigit(evaluationText.FirstOrDefault()))
        {
            var scoreChar = evaluationText.First();
            if (int.TryParse(scoreChar.ToString(), out var fallbackScore) && fallbackScore >= 1 && fallbackScore <= 5)
            {
                score = fallbackScore;
                return true;
            }
        }

        return false;
    }
}