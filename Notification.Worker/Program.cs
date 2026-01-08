using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Notification.Worker.Consumers;
using Notification.Worker.Workers;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions; 
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks; 
using Common.Domain.Interfaces;
using Common.Persistence.Repositories;
using Notification.Worker.Interfaces;
using Notification.Worker.Services;
using Notification.Worker.Configurations;

// Parámetros RabbitMQ
const int maxRetries = 10;
const int delayMs = 5000; // 5 segundos

var hostBuilder = Host.CreateDefaultBuilder(args);

IHost host = hostBuilder
    .ConfigureServices((hostContext, services) =>
    {
        IConfiguration configuration = hostContext.Configuration;

        // ------------------------------------
        // 1. Configuracion de MailSettings
        // ------------------------------------
        var mailSettingsSection = configuration.GetSection(nameof(MailSettings));
        var mailSettings = mailSettingsSection.Get<MailSettings>();

        if (mailSettings != null)
        {
            services.AddSingleton(mailSettings);
        }
        else
        {
            Console.WriteLine(" ADVERTENCIA: MailSettings no encontrada. El envío de correo fallará.");
        }


        // ----------------------------------------------------
        // 2. Conexión a RabbtitMQ con reintentos
        // ----------------------------------------------------
        var factory = new ConnectionFactory
        {
            HostName = configuration["rabbitmq:host"] ?? "rabbitmq",
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

        if (connection != null)
        {
            services.AddSingleton(connection);
        }

        // ------------------------------------
        // Base de Datos y Repositorios
        // ------------------------------------
        string connectionString = configuration.GetConnectionString("DefaultConnection")
                                 ?? throw new InvalidOperationException("DefaultConnection no configurado.");

        services.AddSingleton<IJobStatusRepository, JobStatusRepository>();

        services.AddSingleton<IEmailService, EmailService>();
        services.AddSingleton<JobFinishedConsumer>();
        services.AddHostedService<NotificationWorker>();
    })
    .Build();

await host.RunAsync();