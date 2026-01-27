using DataProcessor.Infrastructure.Interfaces;
using DataProcessor.Infrastructure.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Common.Persistence;
using MediatR;
using DataProcessor.Application.Features.CargaMasiva.Commands;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System;
using System.Threading.Tasks;
using Common.Domain.Interfaces;
using DataProcessor.Worker;
using DataProcessor.Worker.Workers;
using DataProcessor.Worker.Workers.Services;

const int maxRetries = 10;
const int delayMs = 5000; // 5 segundos

var hostBuilder = Host.CreateDefaultBuilder(args);

IHost host = hostBuilder
    .ConfigureServices((hostContext, services) =>
    {
        IConfiguration configuration = hostContext.Configuration;

        // --- CONFIGURACIÓN DE REDIS ---
        // Usamos el host 'redis' que es el nombre del servicio en docker-compose
        var redisConn = configuration["Redis:RedisConnection"]
                        ?? "redis:6379,password=Peru2412,abortConnect=false";

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConn;
            options.InstanceName = "AtlanticApp:"; // Debe ser igual al de la API para que el borrado funcione
        });
        Console.WriteLine("Servicios de Redis configurados en el Worker.");

        // --- CONFIGURACIÓN DE RABBITMQ ---
        var factory = new ConnectionFactory()
        {
            HostName = configuration["rabbitmq:host"] ?? "localhost",
            Port = int.Parse(configuration["rabbitmq:port"] ?? "5672"),
            UserName = configuration["rabbitmq:username"] ?? "guest",
            Password = configuration["rabbitmq:password"] ?? "guest"
        };

        IConnection connection = null;
        int retryCount = 0;

        Console.WriteLine("Iniciando conexión persistente a RabbitMQ...");

        while (retryCount < maxRetries && connection == null)
        {
            try
            {
                Console.WriteLine($"Intentando conectar a RabbitMQ ({factory.HostName}:{factory.Port}). Intento {retryCount + 1}/{maxRetries}...");
                connection = factory.CreateConnection();
                Console.WriteLine("Conexión a RabbitMQ exitosa.");
            }
            catch (BrokerUnreachableException)
            {
                retryCount++;
                if (retryCount < maxRetries)
                {
                    Console.WriteLine($"Conexión fallida. Reintentando en {delayMs / 1000} segundos...");
                    Task.Delay(delayMs).Wait();
                }
                else
                {
                    Console.WriteLine("ERROR FATAL: No se pudo conectar a RabbitMQ después de múltiples reintentos.");
                    throw;
                }
            }
        }

        // --- REGISTRO DE SERVICIOS RESTANTES ---
        services.AddPersistence(configuration);
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<ProcessFileCommand>());

        if (connection != null)
        {
            services.AddSingleton(connection);
        }

        services.AddSingleton<IMessageConsumer, RabbitMQConsumer>();
        services.AddHostedService<DataProcessor.Worker.Workers.FileProcessingWorker>();
        services.AddScoped<IFileDownloadService, LocalFileDownloadService>();
        services.AddSingleton<FileIngestor.Application.Interfaces.IMessagePublisher, DataProcessor.Worker.Workers.Services.RabbitMqPublisher>();

        services.AddHttpClient();
    })
    .Build();

await host.RunAsync();
