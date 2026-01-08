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
// Parámetros de reintento para la conexión a RabbitMQ
const int maxRetries = 10;
const int delayMs = 5000; // 5 segundos

var hostBuilder = Host.CreateDefaultBuilder(args);

IHost host = hostBuilder
    .ConfigureServices((hostContext, services) =>
    {
        IConfiguration configuration = hostContext.Configuration;

        var factory = new ConnectionFactory()
        {
            HostName = configuration["rabbitmq:host"] ?? "localhost",
            Port = int.Parse(configuration["rabbitmq:port"] ?? "5672"),
            UserName = configuration["rabbitmq:username"] ?? "guest",
            Password = configuration["rabbitmq:password"] ?? "guest"
        };

        IConnection connection = null;
        int retryCount = 0;

        // Muestra logs en consola antes de que el logger esté listo
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
                    throw; // Detiene la aplicación si no puede conectar
                }
            }
        }

        services.AddPersistence(configuration);
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<ProcessFileCommand>());

        // Registrar la CONEXIÓN de RabbitMQ establecida (IConnection)
        if (connection != null)
        {
            services.AddSingleton(connection);
        }

        // Consumidor RabbitMQ
        services.AddSingleton<IMessageConsumer, RabbitMQConsumer>();
        services.AddScoped<IFileDownloadService, LocalFileDownloadService>();
        
    })
    .Build();

// Ejecutar el Worker
await host.RunAsync();
