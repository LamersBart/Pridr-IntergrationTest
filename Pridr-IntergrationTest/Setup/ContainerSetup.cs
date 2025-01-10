using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Npgsql;

namespace Pridr_IntergrationTest.Setup;

public class ContainerSetup : IAsyncDisposable
{
    private INetwork _network;
    public IContainer UserService  { get; set; }
    public IContainer EventService  { get; set; }
    public IContainer ChatService  { get; set; }
    public PostgreSqlContainer PostgresContainer { get; set; }
    public RabbitMqContainer RabbitMqContainer { get; set; }
    public string userServiceConnString { get; set; }
    public string chatServiceConnString { get; set; }
    public string eventServiceConnString { get; set; }
    public string postgressHost { get; set; }
    public string postgressPort { get; set; }
    public string rabbitMQHost { get; set; }
    public string rabbitMQPort { get; set; }

    
    public async Task StartServicesAsync()
    {
        _network = new NetworkBuilder()
            .WithName("pridr-network")
            .WithReuse(true)
            .Build();
        
        // RabbitMQ container
        RabbitMqContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:3-management") 
            .WithUsername("testuser")
            .WithPassword("testpassword")
            .WithPortBinding(5672, true)
            .WithPortBinding(15672, true)
            .WithNetwork(_network)
            .WithNetworkAliases("rabbitmq")
            .Build();
        await RabbitMqContainer.StartAsync();
        rabbitMQHost = RabbitMqContainer.Hostname;
        rabbitMQPort = RabbitMqContainer.GetMappedPublicPort(5672).ToString();
        Console.WriteLine($"RabbitMQ Host: {rabbitMQHost}");
        Console.WriteLine($"RabbitMQ Port: " + rabbitMQPort);
        Console.WriteLine($"RabbitMQ Management Port: " + RabbitMqContainer.GetMappedPublicPort(15672));

        
        // PostgreSQL container
        PostgresContainer = new PostgreSqlBuilder()
            .WithPortBinding(5432, true)
            .WithDatabase("postgres")
            .WithUsername("testuser")
            .WithPassword("testpassword")
            .WithNetwork(_network)
            .WithNetworkAliases("postgres")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .Build();
        await PostgresContainer.StartAsync();
        postgressHost = PostgresContainer.Hostname;
        postgressPort = PostgresContainer.GetMappedPublicPort(5432).ToString();
        Console.WriteLine($"PostgreSQL Host: {postgressHost}");
        Console.WriteLine($"PostgreSQL Port: " + postgressPort);
        await CreateDatabasesAsync();
        
        // UserService
        UserService = new ContainerBuilder()
            .WithImage("lamersbart/pridr-userservice:latest")
            .WithEnvironment("PGHOST", "postgres")
            .WithEnvironment("PGPORT", "5432")
            .WithEnvironment("PGUSER", "testuser")
            .WithEnvironment("PGPASS", "testpassword")
            .WithEnvironment("PGDB", "user_db")
            .WithEnvironment("ENCRYPTION_KEY", "YourSecurePassword1234")
            .WithEnvironment("MQHOST", "rabbitmq")
            .WithEnvironment("MQPORT", "5672")
            .WithEnvironment("MQUSER", "testuser")
            .WithEnvironment("MQPASS", "testpassword")
            .WithEnvironment("DISABLE_AUTH", "true")
            .WithPortBinding(0, 8080)
            .WithExposedPort(8080)
            .WithNetwork(_network)
            .WithNetworkAliases("userService")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(8080))
            .Build();
        await UserService.StartAsync();
        Console.WriteLine($"UserService Hostname: {UserService.Hostname}");
        Console.WriteLine($"UserService Port: {UserService.GetMappedPublicPort(8080)}");
        Console.WriteLine($"UserService container state: {UserService.State}");


        
        // EventService
        EventService = new ContainerBuilder()
            .WithImage("lamersbart/pridr-eventservice:latest")
            .WithEnvironment("PGHOST", "postgres")
            .WithEnvironment("PGPORT", "5432")
            .WithEnvironment("PGUSER", "testuser")
            .WithEnvironment("PGPASS", "testpassword")
            .WithEnvironment("PGDB", "event_db")
            .WithEnvironment("MQHOST", "rabbitmq")
            .WithEnvironment("MQPORT", "5672")
            .WithEnvironment("MQUSER", "testuser")
            .WithEnvironment("MQPASS", "testpassword")
            .WithEnvironment("DISABLE_AUTH", "true")
            .WithPortBinding(0, 8080)
            .WithExposedPort(8080)
            .WithNetwork(_network)
            .WithNetworkAliases("eventService")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(8080))
            .Build();
        await EventService.StartAsync();
        
        // ChatService
        ChatService = new ContainerBuilder()
            .WithImage("lamersbart/pridr-chatservice:latest")
            .WithEnvironment("PGHOST", "postgres")
            .WithEnvironment("PGPORT", "5432")
            .WithEnvironment("PGUSER", "testuser")
            .WithEnvironment("PGPASS", "testpassword")
            .WithEnvironment("PGDB", "chat_db")
            .WithEnvironment("ENCRYPTION_KEY", "YourSecurePassword1234")
            .WithEnvironment("MQHOST", "rabbitmq")
            .WithEnvironment("MQPORT", "5672")
            .WithEnvironment("MQUSER", "testuser")
            .WithEnvironment("MQPASS", "testpassword")
            .WithPortBinding(0, 8080)
            .WithExposedPort(8080)
            .WithNetwork(_network)
            .WithNetworkAliases("chatService")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(8080))
            .Build();
        await ChatService.StartAsync();
        await Task.Delay(5000);
    }

    private async Task CreateDatabasesAsync() {
        var connectionString = PostgresContainer.GetConnectionString();
        const string createDbSql = @"
            CREATE DATABASE chat_db;
            CREATE DATABASE event_db;
            CREATE DATABASE user_db;
        ";

        Console.WriteLine($"--> CreateDatabasesAsync: conn-string: {connectionString}");
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(createDbSql, connection);
        await command.ExecuteNonQueryAsync();
        
        string pgUser = "testuser";
        string pgPass = "testpassword";
        userServiceConnString = $"Host={postgressHost};Port={postgressPort};Username={pgUser};Password={pgPass};Database=user_db";
        eventServiceConnString = $"Host={postgressHost};Port={postgressPort};Username={pgUser};Password={pgPass};Database=event_db";
        chatServiceConnString = $"Host={postgressHost};Port={postgressPort};Username={pgUser};Password={pgPass};Database=chat_db";
    }
    
    public async ValueTask DisposeAsync()
    {
        await PostgresContainer.DisposeAsync();
        await RabbitMqContainer.DisposeAsync();
        await UserService.DisposeAsync();
        await EventService.DisposeAsync();
        await ChatService.DisposeAsync();
        await _network.DisposeAsync();
    }
}