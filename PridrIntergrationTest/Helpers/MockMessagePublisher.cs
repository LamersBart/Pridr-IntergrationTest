using System.Text;
using RabbitMQ.Client;

namespace PridrIntergrationTest.Helpers;

public class MockMessagePublisher
{
    public void PublishMessage(string message, string hostname, int port, string typeEvent)
    {
        var factory = new ConnectionFactory
        {
            HostName = hostname,
            Port = port,
            UserName = "testuser",
            Password = "testpassword"
        };

        switch (typeEvent)
        {
            case "NewUser":
            {
                using var connection = factory.CreateConnection();
                using var channel = connection.CreateModel();
                channel.ConfirmSelect();
                var queueName = "example_queue";
                channel.QueueDeclare(queue: queueName, durable: false, exclusive: false, autoDelete: false,
                    arguments: null);
                channel.QueueBind(queue: queueName, exchange: "amq.topic",
                    routingKey: "KK.EVENT.CLIENT.pridr.SUCCESS.#.REGISTER");


                Console.WriteLine("--> Trying to send message on messagebus..." + message);
                var body = Encoding.UTF8.GetBytes(message);
                channel.BasicPublish(exchange: "amq.topic", routingKey: "KK.EVENT.CLIENT.pridr.SUCCESS.#.REGISTER",
                    mandatory: true, basicProperties: null, body: body);

                if (!channel.WaitForConfirms(TimeSpan.FromSeconds(10)))
                {
                    throw new Exception("--> Message was not confirmed by RabbitMQ.");
                }

                Console.WriteLine("--> Message confirmed by RabbitMQ.");
            }
                break;
            case "DeleteUser":
            {
                using var connection = factory.CreateConnection();
                using var channel = connection.CreateModel();
                channel.ConfirmSelect();
                var queueName = "example_queue";
                channel.QueueDeclare(queue: queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
                channel.QueueBind(queue: queueName, exchange: "amq.topic", routingKey: "KK.EVENT.CLIENT.pridr.SUCCESS.#.DELETE_ACCOUNT");
        

                Console.WriteLine("--> Trying to send message on messagebus..." + message);
                var body = Encoding.UTF8.GetBytes(message);
                channel.BasicPublish(exchange: "amq.topic", routingKey: "KK.EVENT.CLIENT.pridr.SUCCESS.#.DELETE_ACCOUNT", mandatory: true, basicProperties: null, body: body);
       
                if (!channel.WaitForConfirms(TimeSpan.FromSeconds(10)))
                {
                    throw new Exception("--> Message was not confirmed by RabbitMQ.");
                }
                Console.WriteLine("--> Message confirmed by RabbitMQ.");
            }
                break;
        }
    }
}