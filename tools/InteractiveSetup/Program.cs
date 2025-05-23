﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.KernelMemory.InteractiveSetup.Service;
using Microsoft.KernelMemory.InteractiveSetup.Services;
using Microsoft.KernelMemory.InteractiveSetup.UI;

namespace Microsoft.KernelMemory.InteractiveSetup;

public static class Program
{
    public static void Main(string[] args)
    {
        var ctx = new Context();

        // If args is not empty, then the user is asking to configure a specific list of services
        if (args.Length > 0)
        {
            if (args[0] == "--check")
            {
                Doctor.Check.Run();
            }
            else
            {
                ConfigureItem(ctx, args);
            }

            SetupUI.Exit();
        }

        try
        {
            KMService.Setup(ctx);
            Webservice.Setup(ctx);

            // Orchestration
            QueuesTypeSetup(ctx);
            AzureQueue.Setup(ctx);
            RabbitMQ.Setup(ctx);
            SimpleQueues.Setup(ctx);

            // Document Storage
            DocumentStorageTypeSetup(ctx);
            AzureBlobs.Setup(ctx);
            AWSS3.Setup(ctx);
            MongoDbAtlasDocumentStorage.Setup(ctx);
            SimpleFileStorage.Setup(ctx);

            // Image support
            OCRTypeSetup(ctx);
            AzureAIDocIntel.Setup(ctx);

            // Embedding generation
            EmbeddingGeneratorSetup(ctx);
            AzureOpenAIEmbedding.Setup(ctx);
            OpenAI.Setup(ctx);

            // Memory DB
            MemoryDbTypeSetup(ctx);
            AzureAISearch.Setup(ctx);
            MongoDbAtlasMemoryDb.Setup(ctx);
            Postgres.Setup(ctx);
            Qdrant.Setup(ctx);
            Redis.Setup(ctx);
            SimpleVectorDb.Setup(ctx);

            // Text generation
            TextGeneratorTypeSetup(ctx);
            AzureOpenAIText.Setup(ctx);
            OpenAI.Setup(ctx);
            LlamaSharp.Setup(ctx);
            Ollama.Setup(ctx);

            Logger.Setup();

            Console.WriteLine("== Done! :-)\n");
            Console.WriteLine("== You can start the service with: dotnet run\n");
        }
        catch (Exception e)
        {
            Console.WriteLine($"== Error: {e.GetType().FullName}");
            Console.WriteLine($"== {e.Message}");
        }

        SetupUI.Exit();
    }

    private static void ConfigureItem(Context ctx, string[] items)
    {
        foreach (var itemName in items)
        {
            switch (itemName)
            {
                case string x when x.Equals("MemoryDbType", StringComparison.OrdinalIgnoreCase):
                    MemoryDbTypeSetup(ctx);
                    break;

                case string x when x.Equals("TextGeneratorType", StringComparison.OrdinalIgnoreCase):
                    TextGeneratorTypeSetup(ctx);
                    break;

                case string x when x.Equals("QueuesType", StringComparison.OrdinalIgnoreCase):
                    QueuesTypeSetup(ctx);
                    break;

                case string x when x.Equals("DocumentStorageType", StringComparison.OrdinalIgnoreCase):
                    DocumentStorageTypeSetup(ctx);
                    break;

                case string x when x.Equals("AzureAISearch", StringComparison.OrdinalIgnoreCase):
                    AzureAISearch.Setup(ctx, true);
                    break;

                case string x when x.Equals("AzureOpenAIEmbedding", StringComparison.OrdinalIgnoreCase):
                    AzureOpenAIEmbedding.Setup(ctx, true);
                    break;

                case string x when x.Equals("AzureOpenAIText", StringComparison.OrdinalIgnoreCase):
                    AzureOpenAIText.Setup(ctx, true);
                    break;

                case string x when x.Equals("LlamaSharp", StringComparison.OrdinalIgnoreCase):
                    LlamaSharp.Setup(ctx, true);
                    break;

                case string x when x.Equals("MongoDbAtlas", StringComparison.OrdinalIgnoreCase):
                    MongoDbAtlasMemoryDb.Setup(ctx, true);
                    break;

                case string x when x.Equals("OpenAI", StringComparison.OrdinalIgnoreCase):
                    OpenAI.Setup(ctx, true);
                    break;

                case string x when x.Equals("Postgres", StringComparison.OrdinalIgnoreCase):
                    Postgres.Setup(ctx, true);
                    break;

                case string x when x.Equals("Qdrant", StringComparison.OrdinalIgnoreCase):
                    Qdrant.Setup(ctx, true);
                    break;

                case string x when x.Equals("RabbitMQ", StringComparison.OrdinalIgnoreCase):
                    RabbitMQ.Setup(ctx, true);
                    break;

                case string x when x.Equals("Redis", StringComparison.OrdinalIgnoreCase):
                    Redis.Setup(ctx, true);
                    break;

                case string x when x.Equals("SimpleVectorDb", StringComparison.OrdinalIgnoreCase):
                    SimpleVectorDb.Setup(ctx, true);
                    break;
            }
        }
    }

    private static void EmbeddingGeneratorSetup(Context ctx)
    {
        var config = AppSettings.GetCurrentConfig();

        SetupUI.AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "When importing data, generate embeddings, or let the memory Db class take care of it?",
            Options =
            [
                new("Yes, generate embeddings", config.DataIngestion.EmbeddingGenerationEnabled, () =>
                {
                    AppSettings.Change(x => x.DataIngestion.EmbeddingGenerationEnabled = true);
                    ctx.CfgEmbeddingGenerationEnabled.Value = true;
                }),

                new("No, my memory Db class/engine takes care of it", !config.DataIngestion.EmbeddingGenerationEnabled, () =>
                {
                    AppSettings.Change(x => x.DataIngestion.EmbeddingGenerationEnabled = false);
                    ctx.CfgEmbeddingGenerationEnabled.Value = false;
                })
            ]
        });

        SetupUI.AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "When searching for text and/or answers, which embedding generator should be used for vector search?",
            Options =
            [
                new("Azure OpenAI embedding model", config.Retrieval.EmbeddingGeneratorType == "AzureOpenAIEmbedding", () =>
                {
                    AppSettings.Change(x =>
                    {
                        x.Retrieval.EmbeddingGeneratorType = "AzureOpenAIEmbedding";
                        x.DataIngestion.EmbeddingGeneratorTypes = ctx.CfgEmbeddingGenerationEnabled.Value
                            ? [x.Retrieval.EmbeddingGeneratorType]
                            : [];
                    });
                    ctx.CfgAzureOpenAIEmbedding.Value = true;
                }),

                new("OpenAI embedding model", config.Retrieval.EmbeddingGeneratorType == "OpenAI", () =>
                {
                    AppSettings.Change(x =>
                    {
                        x.Retrieval.EmbeddingGeneratorType = "OpenAI";
                        x.DataIngestion.EmbeddingGeneratorTypes = ctx.CfgEmbeddingGenerationEnabled.Value
                            ? [x.Retrieval.EmbeddingGeneratorType]
                            : [];
                    });
                    ctx.CfgOpenAI.Value = true;
                }),

                new("Ollama service", config.Retrieval.EmbeddingGeneratorType == "Ollama", () =>
                {
                    AppSettings.Change(x =>
                    {
                        x.Retrieval.EmbeddingGeneratorType = "Ollama";
                        x.DataIngestion.EmbeddingGeneratorTypes = ctx.CfgEmbeddingGenerationEnabled.Value
                            ? [x.Retrieval.EmbeddingGeneratorType]
                            : [];
                    });
                    ctx.CfgOllamaEmbedding.Value = true;
                }),

                new("LlamaSharp library", config.Retrieval.EmbeddingGeneratorType == "LlamaSharp", () =>
                {
                    AppSettings.Change(x =>
                    {
                        x.Retrieval.EmbeddingGeneratorType = "LlamaSharp";
                        x.DataIngestion.EmbeddingGeneratorTypes = ctx.CfgEmbeddingGenerationEnabled.Value
                            ? [x.Retrieval.EmbeddingGeneratorType]
                            : [];
                    });
                    ctx.CfgLlamaSharpEmbedding.Value = true;
                }),

                new("None/Custom (manually set with code)", string.IsNullOrEmpty(config.Retrieval.EmbeddingGeneratorType), () =>
                {
                    AppSettings.Change(x =>
                    {
                        x.Retrieval.EmbeddingGeneratorType = "";
                        x.DataIngestion.EmbeddingGeneratorTypes = [];
                    });
                }),

                new("-exit-", false, SetupUI.Exit)
            ]
        });
    }

    private static void TextGeneratorTypeSetup(Context ctx)
    {
        var config = AppSettings.GetCurrentConfig();

        SetupUI.AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "When generating answers and synthetic data, which LLM text generator should be used?",
            Options =
            [
                new("Azure OpenAI text/chat model", config.TextGeneratorType == "AzureOpenAIText", () =>
                {
                    AppSettings.Change(x => { x.TextGeneratorType = "AzureOpenAIText"; });
                    ctx.CfgAzureOpenAIText.Value = true;
                }),

                new("OpenAI text/chat model", config.TextGeneratorType == "OpenAI", () =>
                {
                    AppSettings.Change(x => { x.TextGeneratorType = "OpenAI"; });
                    ctx.CfgOpenAI.Value = true;
                }),

                new("Ollama service", config.TextGeneratorType == "Ollama", () =>
                {
                    AppSettings.Change(x => { x.TextGeneratorType = "Ollama"; });
                    ctx.CfgOllamaText.Value = true;
                }),

                new("LlamaSharp library", config.TextGeneratorType == "LlamaSharp", () =>
                {
                    AppSettings.Change(x => { x.TextGeneratorType = "LlamaSharp"; });
                    ctx.CfgLlamaSharpText.Value = true;
                }),

                new("None/Custom (manually set with code)", string.IsNullOrEmpty(config.TextGeneratorType), () =>
                {
                    AppSettings.Change(x => { x.TextGeneratorType = ""; });
                }),

                new("-exit-", false, SetupUI.Exit)
            ]
        });
    }

    private static void OCRTypeSetup(Context ctx)
    {
        var config = AppSettings.GetCurrentConfig();

        SetupUI.AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "Which service should be used to extract text from images?",
            Options =
            [
                new("None", config.DataIngestion.ImageOcrType == "None", () =>
                {
                    AppSettings.Change(x => { x.DataIngestion.ImageOcrType = "None"; });
                }),

                new("Azure AI Document Intelligence", config.DataIngestion.ImageOcrType == "AzureAIDocIntel", () =>
                {
                    AppSettings.Change(x => { x.DataIngestion.ImageOcrType = "AzureAIDocIntel"; });
                    ctx.CfgAzureAIDocIntel.Value = true;
                }),

                new("-exit-", false, SetupUI.Exit)
            ]
        });
    }

    private static void QueuesTypeSetup(Context ctx)
    {
        if (!ctx.CfgQueue.Value) { return; }

        var config = AppSettings.GetCurrentConfig();

        ctx.CfgQueue.Value = false;
        SetupUI.AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "Which queue service will be used?",
            Options =
            [
                new("Azure Queue",
                    config.DataIngestion.DistributedOrchestration.QueueType == "AzureQueues",
                    () =>
                    {
                        AppSettings.Change(x => { x.DataIngestion.DistributedOrchestration.QueueType = "AzureQueues"; });
                        ctx.CfgAzureQueue.Value = true;
                    }),

                new("RabbitMQ",
                    config.DataIngestion.DistributedOrchestration.QueueType == "RabbitMQ",
                    () =>
                    {
                        AppSettings.Change(x => { x.DataIngestion.DistributedOrchestration.QueueType = "RabbitMQ"; });
                        ctx.CfgRabbitMq.Value = true;
                    }),

                new("SimpleQueues volatile (only for tests, data stored in memory)",
                    config.DataIngestion.DistributedOrchestration.QueueType == "SimpleQueues"
                    && (!config.Services.TryGetValue("SimpleQueues", out _) ||
                        (config.Services.TryGetValue("SimpleQueues", out var c1) && (string)c1["StorageType"] == "Volatile")),
                    () =>
                    {
                        AppSettings.Change(x => { x.DataIngestion.DistributedOrchestration.QueueType = "SimpleQueues"; });
                        ctx.CfgSimpleQueuesVolatile.Value = true;
                    }),

                new("SimpleQueues persistent (only for tests, data stored on disk)",
                    config.DataIngestion.DistributedOrchestration.QueueType == "SimpleQueues"
                    && config.Services.TryGetValue("SimpleQueues", out var c2) && (string)c2["StorageType"] == "Disk",
                    () =>
                    {
                        AppSettings.Change(x => { x.DataIngestion.DistributedOrchestration.QueueType = "SimpleQueues"; });
                        ctx.CfgSimpleQueuesPersistent.Value = true;
                    }),

                new("-exit-", false, SetupUI.Exit)
            ]
        });
    }

    private static void DocumentStorageTypeSetup(Context ctx)
    {
        if (!ctx.CfgDocumentStorage.Value) { return; }

        var config = AppSettings.GetCurrentConfig();

        SetupUI.AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "Where should the service store files? A persistent storage is required to handle updates, downloads, etc.",
            Options =
            [
                new("Azure Blobs",
                    config.DocumentStorageType == "AzureBlobs",
                    () =>
                    {
                        AppSettings.Change(x => { x.DocumentStorageType = "AzureBlobs"; });
                        ctx.CfgAzureBlobs.Value = true;
                    }),

                new("AWS S3",
                    config.DocumentStorageType == "AWSS3",
                    () =>
                    {
                        AppSettings.Change(x => { x.DocumentStorageType = "AWSS3"; });
                        ctx.CfgAWSS3.Value = true;
                    }),

                new("MongoDB Atlas",
                    config.DocumentStorageType == "MongoDbAtlas",
                    () =>
                    {
                        AppSettings.Change(x => { x.DocumentStorageType = "MongoDbAtlas"; });
                        ctx.CfgMongoDbAtlasDocumentStorage.Value = true;
                    }),

                new("SimpleFileStorage volatile (only for tests, data stored in memory)",
                    config.DocumentStorageType == "SimpleFileStorage"
                    && (!config.Services.TryGetValue("SimpleFileStorage", out _) ||
                        (config.Services.TryGetValue("SimpleFileStorage", out var c1) && (string)c1["StorageType"] == "Volatile")),
                    () =>
                    {
                        AppSettings.Change(x => { x.DocumentStorageType = "SimpleFileStorage"; });
                        ctx.CfgSimpleFileStorageVolatile.Value = true;
                    }),

                new("SimpleFileStorage persistent (only for tests, data stored on disk)",
                    config.DocumentStorageType == "SimpleFileStorage"
                    && config.Services.TryGetValue("SimpleFileStorage", out var c2) && (string)c2["StorageType"] == "Disk",
                    () =>
                    {
                        AppSettings.Change(x => { x.DocumentStorageType = "SimpleFileStorage"; });
                        ctx.CfgSimpleFileStoragePersistent.Value = true;
                    }),

                new("-exit-", false, SetupUI.Exit)
            ]
        });
    }

    private static void MemoryDbTypeSetup(Context ctx)
    {
        var config = AppSettings.GetCurrentConfig();

        SetupUI.AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "When searching for answers, which memory DB service contains the records to search?",
            Options =
            [
                new("Azure AI Search",
                    config.Retrieval.MemoryDbType == "AzureAISearch",
                    () =>
                    {
                        AppSettings.Change(x =>
                        {
                            x.Retrieval.MemoryDbType = "AzureAISearch";
                            x.DataIngestion.MemoryDbTypes = [x.Retrieval.MemoryDbType];
                        });
                        ctx.CfgAzureAISearch.Value = true;
                    }),

                new("Postgres",
                    config.Retrieval.MemoryDbType == "Postgres",
                    () =>
                    {
                        AppSettings.Change(x =>
                        {
                            x.Retrieval.MemoryDbType = "Postgres";
                            x.DataIngestion.MemoryDbTypes = [x.Retrieval.MemoryDbType];
                        });
                        ctx.CfgPostgres.Value = true;
                    }),

                new("MongoDB Atlas",
                    config.Retrieval.MemoryDbType == "MongoDbAtlas",
                    () =>
                    {
                        AppSettings.Change(x =>
                        {
                            x.Retrieval.MemoryDbType = "MongoDbAtlas";
                            x.DataIngestion.MemoryDbTypes = [x.Retrieval.MemoryDbType];
                        });
                        ctx.CfgMongoDbAtlasMemory.Value = true;
                    }),

                new("Redis",
                    config.Retrieval.MemoryDbType == "Redis",
                    () =>
                    {
                        AppSettings.Change(x =>
                        {
                            x.Retrieval.MemoryDbType = "Redis";
                            x.DataIngestion.MemoryDbTypes = [x.Retrieval.MemoryDbType];
                        });
                        ctx.CfgRedis.Value = true;
                    }),

                new("Qdrant",
                    config.Retrieval.MemoryDbType == "Qdrant",
                    () =>
                    {
                        AppSettings.Change(x =>
                        {
                            x.Retrieval.MemoryDbType = "Qdrant";
                            x.DataIngestion.MemoryDbTypes = [x.Retrieval.MemoryDbType];
                        });
                        ctx.CfgQdrant.Value = true;
                    }),

                new("SimpleVectorDb volatile (only for tests, data stored in memory)",
                    config.Retrieval.MemoryDbType == "SimpleVectorDb"
                    && (!config.Services.TryGetValue("SimpleVectorDb", out _) ||
                        (config.Services.TryGetValue("SimpleVectorDb", out var c1) && (string)c1["StorageType"] == "Volatile")),
                    () =>
                    {
                        AppSettings.Change(x =>
                        {
                            x.Retrieval.MemoryDbType = "SimpleVectorDb";
                            x.DataIngestion.MemoryDbTypes = [x.Retrieval.MemoryDbType];
                        });
                        ctx.CfgSimpleVectorDbVolatile.Value = true;
                    }),

                new("SimpleVectorDb persistent (only for tests, data stored on disk)",
                    config.Retrieval.MemoryDbType == "SimpleVectorDb"
                    && config.Services.TryGetValue("SimpleVectorDb", out var c2) && (string)c2["StorageType"] == "Disk",
                    () =>
                    {
                        AppSettings.Change(x =>
                        {
                            x.Retrieval.MemoryDbType = "SimpleVectorDb";
                            x.DataIngestion.MemoryDbTypes = [x.Retrieval.MemoryDbType];
                        });
                        ctx.CfgSimpleVectorDbPersistent.Value = true;
                    }),

                new("None/Custom (manually set in code)",
                    string.IsNullOrEmpty(config.Retrieval.MemoryDbType),
                    () =>
                    {
                        AppSettings.Change(x =>
                        {
                            x.Retrieval.MemoryDbType = "";
                            x.DataIngestion.MemoryDbTypes = [];
                        });
                    }),

                new("-exit-", false, SetupUI.Exit)
            ]
        });
    }
}
