using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace LibraryManagement.NotificationService;

public class Worker(ILogger<Worker> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory()
        {
            HostName = "localhost"
        };

        var connection = factory.CreateConnection();

        var channel = connection.CreateModel();

        channel.QueueDeclare(
            queue: "book_issue_queue",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var consumer = new EventingBasicConsumer(channel);

        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();

            var message = Encoding.UTF8.GetString(body);

            logger.LogInformation(
                "Issue notification received: {Message}",
                message);
        };

        channel.BasicConsume(
            queue: "book_issue_queue",
            autoAck: true,
            consumer: consumer);

        channel.QueueDeclare(
            queue: "book_return_queue",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var returnConsumer = new EventingBasicConsumer(channel);

        returnConsumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();

            var message = Encoding.UTF8.GetString(body);

            logger.LogInformation(
                "Return notification received: {Message}",
                message);
        };

        channel.BasicConsume(
            queue: "book_return_queue",
            autoAck: true,
            consumer: returnConsumer);

        channel.QueueDeclare(
            queue: "user_registration_queue",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var registrationConsumer = new EventingBasicConsumer(channel);

        registrationConsumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();

            var message = Encoding.UTF8.GetString(body);

            logger.LogInformation(
                "User registration notification received: {Message}",
                message);
        };

        channel.BasicConsume(
            queue: "user_registration_queue",
            autoAck: true,
            consumer: registrationConsumer);

        return Task.CompletedTask;
    }
}