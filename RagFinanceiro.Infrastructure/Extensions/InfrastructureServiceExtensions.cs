#pragma warning disable SKEXP0001, SKEXP0010, SKEXP0020

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;
using RagFinanceiro.Application.UseCases.DeleteContract;
using RagFinanceiro.Application.UseCases.IngestContract;
using RagFinanceiro.Application.UseCases.QueryContract;
using RagFinanceiro.Domain.Repositories;
using RagFinanceiro.Domain.Services;
using RagFinanceiro.Infrastructure.AI;
using RagFinanceiro.Infrastructure.Pdf;
using RagFinanceiro.Infrastructure.Persistence;

namespace RagFinanceiro.Infrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var openAiKey = GetRequiredConfiguration(configuration, "OpenAI:Key");
        var chatModel = GetRequiredConfiguration(configuration, "OpenAI:ChatModel");
        var embeddingModel = GetRequiredConfiguration(configuration, "OpenAI:EmbeddingModel");
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' nao configurada.");

        // Semantic Kernel
        var kernel = Kernel.CreateBuilder()
            .AddOpenAIChatCompletion(chatModel, openAiKey)
            .Build();

        services.AddSingleton(kernel);

        // Embedding service
        ITextEmbeddingGenerationService embeddingService = new OpenAITextEmbeddingGenerationService(
            embeddingModel, openAiKey);
        services.AddSingleton(embeddingService);

        // ContractMemoryStore
        services.AddSingleton(sp =>
            new ContractMemoryStore(
                connectionString,
                sp.GetRequiredService<ITextEmbeddingGenerationService>(),
                sp.GetRequiredService<ILogger<ContractMemoryStore>>()));

        // Infrastructure services
        services.AddScoped<IChunkingService, TextChunkingService>();
        services.AddScoped<IPdfReaderService, PdfPigReaderService>();
        services.AddScoped<ILlmQueryService, SemanticKernelQueryService>();
        services.AddScoped<IContractRepository, SemanticKernelContractRepository>();

        // Application handlers
        services.AddScoped<IngestContractHandler>();
        services.AddScoped<QueryContractHandler>();
        services.AddScoped<DeleteContractHandler>();

        return services;
    }

    private static string GetRequiredConfiguration(IConfiguration configuration, string key)
    {
        var value = configuration[key];

        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Configuracao obrigatoria ausente: '{key}'.");

        return value;
    }
}
