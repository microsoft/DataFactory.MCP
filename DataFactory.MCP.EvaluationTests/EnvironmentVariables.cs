// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace DataFactory.MCP.EvaluationTests;

public class EnvironmentVariables
{
    private static readonly IDictionary<string, string> s_environmentVariableCache = new Dictionary<string, string>();

    private static string GetEnvironmentVariable(string variableName)
    {
        if (!s_environmentVariableCache.TryGetValue(variableName, out string? value))
        {
            value =
                Environment.GetEnvironmentVariable(variableName) ??
                throw new Exception($"Environment variable {variableName} not set.");

            s_environmentVariableCache[variableName] = value;
        }

        return value;
    }

    #region Azure OpenAI
    public static string AzureOpenAIEndpoint
        => GetEnvironmentVariable("EVAL_SAMPLE_AZURE_OPENAI_ENDPOINT");

    public static string AzureOpenAIAPIKey
        => GetEnvironmentVariable("EVAL_SAMPLE_AZURE_OPENAI_API_KEY");

    public static string AzureOpenAIModel
        => GetEnvironmentVariable("EVAL_SAMPLE_AZURE_OPENAI_MODEL");
    #endregion
}