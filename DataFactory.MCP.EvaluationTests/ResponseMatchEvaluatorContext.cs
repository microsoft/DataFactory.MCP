// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;

namespace DataFactory.MCP.EvaluationTests;

/// <summary>
/// Represents the context information required by <see cref="ResponseMatchEvaluator"/> to perform its evaluation.
/// </summary>
public sealed class ResponseMatchEvaluatorContext : EvaluationContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResponseMatchEvaluatorContext"/> class.
    /// </summary>
    /// <param name="expectedResponsePattern">Description of the expected response pattern to evaluate against.</param>
    /// <param name="evaluationCriteria">Specific criteria for evaluation (optional).</param>
    /// <param name="scenarioName">Name of the scenario being evaluated (for logging purposes).</param>
    public ResponseMatchEvaluatorContext(
        string expectedResponsePattern,
        string? evaluationCriteria = null,
        string? scenarioName = null)
        : base(
            name: scenarioName ?? "ResponseMatch",
            content: $"Expected Pattern: {expectedResponsePattern}\nCriteria: {evaluationCriteria ?? "Default criteria"}")
    {
        ExpectedResponsePattern = expectedResponsePattern ?? throw new ArgumentNullException(nameof(expectedResponsePattern));
        EvaluationCriteria = evaluationCriteria;
        ScenarioName = scenarioName;
    }

    /// <summary>
    /// Gets the description of the expected response pattern to evaluate against.
    /// </summary>
    public string ExpectedResponsePattern { get; }

    /// <summary>
    /// Gets the specific criteria for evaluation. If null, default criteria will be used.
    /// </summary>
    public string? EvaluationCriteria { get; }

    /// <summary>
    /// Gets the name of the scenario being evaluated (for logging purposes).
    /// </summary>
    public string? ScenarioName { get; }
}