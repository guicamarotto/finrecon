using FinRecon.Infrastructure;
using FinRecon.Worker.Consumers;
using FinRecon.Worker.Parsers;
using MassTransit;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        var config = ctx.Configuration;

        // Infrastructure (DB, MinIO, ReconciliationEngine)
        services.AddInfrastructure(config);

        // File parsers — registered as a collection, consumer resolves all via IEnumerable<IFileParser>
        services.AddTransient<IFileParser, CsvFileParser>();
        services.AddTransient<IFileParser, JsonFileParser>();

        // MassTransit with consumer registration
        services.AddMassTransit(x =>
        {
            x.AddConsumer<ReconciliationJobCreatedConsumer>();

            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(config["RabbitMQ:Host"] ?? "localhost", h =>
                {
                    h.Username(config["RabbitMQ:Username"] ?? "guest");
                    h.Password(config["RabbitMQ:Password"] ?? "guest");
                });

                cfg.ReceiveEndpoint("reconciliation-jobs", e =>
                {
                    // Retry 3 times with 5-second intervals before moving to error queue
                    e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
                    e.ConfigureConsumer<ReconciliationJobCreatedConsumer>(ctx);
                });
            });
        });
    })
    .Build();

await host.RunAsync();
