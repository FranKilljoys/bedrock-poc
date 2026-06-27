using System.Diagnostics.CodeAnalysis;
using Amazon;
using Amazon.BedrockRuntime;
using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.Textract;
using BedRock.POC.Application.Interfaces;
using BedRock.POC.Application.UseCases;
using BedRock.POC.Application.Validators;
using BedRock.POC.Domain.Interfaces;
using BedRock.POC.Infrastructure.Aws;
using BedRock.POC.Infrastructure.Aws.LLM;
using BedRock.POC.Infrastructure.Configuration;
using FluentValidation;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Polly.Timeout;

namespace BedRock.POC.API.Extensions;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddOptions(config);
        services.AddAwsClients();
        services.AddInfrastructure();
        services.AddLlmClients();
        services.AddUseCases();
        services.AddValidation();
        services.AddResiliencePipelines();

        return services;
    }

    private static void AddOptions(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<AwsOptions>(config.GetSection(AwsOptions.Section));
        services.Configure<TextractOptions>(config.GetSection(TextractOptions.Section));
        services.Configure<BedrockModelOptions>(config.GetSection(BedrockModelOptions.Section));
        services.Configure<ExtractionConfiguration>(config.GetSection(ExtractionConfiguration.Section));

        services.AddOptions<AwsOptions>().ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<BedrockModelOptions>().ValidateDataAnnotations().ValidateOnStart();
    }

    private static void AddAwsClients(this IServiceCollection services)
    {
        services.AddSingleton<IAmazonS3>(sp =>
        {
            var region = sp.GetRequiredService<IOptions<AwsOptions>>().Value.Region;
            return new AmazonS3Client(RegionEndpoint.GetBySystemName(region));
        });

        services.AddSingleton<IAmazonTextract>(sp =>
        {
            var region = sp.GetRequiredService<IOptions<AwsOptions>>().Value.Region;
            return new AmazonTextractClient(RegionEndpoint.GetBySystemName(region));
        });

        services.AddSingleton<IAmazonBedrockRuntime>(sp =>
        {
            var region = sp.GetRequiredService<IOptions<AwsOptions>>().Value.Region;
            return new AmazonBedrockRuntimeClient(RegionEndpoint.GetBySystemName(region));
        });

        services.AddSingleton<ITransferUtility>(sp =>
            new TransferUtility(sp.GetRequiredService<IAmazonS3>()));
    }

    private static void AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IDocumentStorage, S3DocumentStorage>();
        services.AddScoped<ITextractAdapter, TextractAdapter>();
        services.AddScoped<ITextractJobPoller, TextractJobPoller>();
        services.AddScoped<ITextractResultCache, TextractResultCache>();
        services.AddScoped<ITextExtractor, TextractTextExtractor>();
        services.AddScoped<IPromptBuilder, BedrockPromptBuilder>();
    }

    private static void AddLlmClients(this IServiceCollection services)
    {
        services.AddScoped<ILanguageModelClient, AnthropicClaudeAdapter>();
        services.AddScoped<ILanguageModelClient, CohereCommandAdapter>();
        services.AddScoped<ILanguageModelClientFactory, BedrockModelFactory>();
    }

    private static void AddUseCases(this IServiceCollection services)
    {
        services.AddScoped<IExtractDocumentUseCase, ExtractDocumentUseCase>();
    }

    private static void AddValidation(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<ExtractDocumentRequestValidator>();
    }

    private static void AddResiliencePipelines(this IServiceCollection services)
    {
        services.AddResiliencePipeline("aws-textract", builder => builder
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential
            })
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromMinutes(5)
            }));

        services.AddResiliencePipeline("aws-bedrock", builder => builder
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 2,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Linear
            })
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(30)
            }));
    }
}
