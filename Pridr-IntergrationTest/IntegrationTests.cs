using System.Net.Http.Headers;
using System.Text;
using Npgsql;
using Pridr_IntergrationTest.Helpers;
using Pridr_IntergrationTest.Setup;

namespace Pridr_IntergrationTest;

[Collection("ContainerTests")]
public class IntegrationTests : IClassFixture<SharedContainerSetup>
{
    private readonly SharedContainerSetup _sharedSetup;
    private readonly MockMessagePublisher _mockMessagePublisher;
    private readonly EncryptionHelper _encryptionHelper;
    
    public IntegrationTests(SharedContainerSetup sharedSetup)
    {
        _sharedSetup = sharedSetup;
        _mockMessagePublisher = new MockMessagePublisher();
        _encryptionHelper = new EncryptionHelper();
    }
    
    [Fact]
    public async Task IntegrationTestFlow()
    {
        // 1. Add User (Test 1)
        await A_Keycloak_Create_User_Event();

        // 2. Update User (Test 2)
        await B_User_Update_Event();

        // 3. Delete User (Test 3)
        await C_User_Create_UserEvent();
        
        // 4. Delete User (Test 3)
        await D_Keycloak_Delete_User_Event();


        async Task A_Keycloak_Create_User_Event()
        {
            // Arrange
            await _sharedSetup.InitializeAsync();
            Console.WriteLine($"UserService container state: {_sharedSetup.ContainerSetup.UserService.State}");
        
            const string message = "{\"@class\":\"com.github.aznamier.keycloak.event.provider.EventClientNotificationMqMsg\",\"time\":1736270017968,\"type\":\"REGISTER\",\"realmId\":\"4ad2a6d1-4fc2-4f01-82b1-31ca3a382fb0\",\"clientId\":\"account-console\",\"userId\":\"a6427685-84e3-4fbf-8716-c94d1053b020\",\"ipAddress\":\"192.168.65.3\",\"details\":{\"auth_method\":\"openid-connect\",\"auth_type\":\"code\",\"register_method\":\"form\",\"redirect_uri\":\"http://localhost:8080/realms/pridr/account/\",\"code_id\":\"ceb07c03-89fc-4ee2-b2c5-c7a477ce3535\",\"email\":\"testjebla@blabla.com\",\"username\":\"testjebla@blabla.com\"}}";
        
            // Act 
            _mockMessagePublisher.PublishMessage(
                message, 
                _sharedSetup.ContainerSetup.RabbitMqContainer.Hostname, 
                _sharedSetup.ContainerSetup.RabbitMqContainer.GetMappedPublicPort(5672),
                "NewUser"
            );
        
            // Assert
            bool isProcessed = false;
            await WaitForConditionAsync(
                async () => isProcessed = await CheckIfUserExists("a6427685-84e3-4fbf-8716-c94d1053b020"),
                TimeSpan.FromSeconds(60),
                TimeSpan.FromMilliseconds(500)
            );
            Assert.True(isProcessed, "Het bericht is niet correct verwerkt. User is niet aangemaakt in de DB");
        }

        async Task B_User_Update_Event()
        {
            // Arrange
            var client = new HttpClient();
            var content = new StringContent(
                "{ \"sexuality\": 1, \"lookingFor\": 0, \"relationStatus\": 1, \"age\": 28, \"weight\": 60, \"height\": 180, \"userName\": \"testuser\" }",
                Encoding.UTF8,"application/json"
            );
            
            // Act 
            var response = await client.PatchAsync($"http://{_sharedSetup.ContainerSetup.UserService.Hostname}:{_sharedSetup.ContainerSetup.UserService.GetMappedPublicPort(8080)}/api/v1/profiles/test", content);
            response.EnsureSuccessStatusCode();
            
            // Assert
            bool isProcessed = false;
            await WaitForConditionAsync(
                async () => isProcessed = await CheckIfUsernameIsUpdated("a6427685-84e3-4fbf-8716-c94d1053b020", "testuser"),
                TimeSpan.FromSeconds(60),
                TimeSpan.FromMilliseconds(500)
            );
            Assert.True(isProcessed, "Het bericht is niet correct verwerkt. User is niet geüpdatet in de DB");
        }
        
        async Task C_User_Create_UserEvent()
        {
            // Arrange
            var client = new HttpClient();
            var content = new StringContent(
                "{\"name\":\"TestEvent\",\"date\":\"2025-01-10T16:14:22.288Z\",\"profileIds\":[\"string\"]}",
                Encoding.UTF8,"application/json"
            );
            
            // Act 
            var response = await client.PostAsync($"http://{_sharedSetup.ContainerSetup.EventService.Hostname}:{_sharedSetup.ContainerSetup.EventService.GetMappedPublicPort(8080)}/api/v1/event/test", content);
            response.EnsureSuccessStatusCode();
            
            // Assert
            bool isProcessed = false;
            await WaitForConditionAsync(
                async () => isProcessed = await CheckIfUserEventExists("a6427685-84e3-4fbf-8716-c94d1053b020"),
                TimeSpan.FromSeconds(60),
                TimeSpan.FromMilliseconds(500)
            );
            Assert.True(isProcessed, "Het bericht is niet correct verwerkt. Geen UserEvent aangemaakt in de DB");
        }
        
        async Task D_Keycloak_Delete_User_Event()
        {
            // Arrange
            const string message = "{\"@class\":\"com.github.aznamier.keycloak.event.provider.EventClientNotificationMqMsg\",\"time\":1736270017968,\"type\":\"DELETE_ACCOUNT\",\"realmId\":\"4ad2a6d1-4fc2-4f01-82b1-31ca3a382fb0\",\"clientId\":\"account-console\",\"userId\":\"a6427685-84e3-4fbf-8716-c94d1053b020\",\"ipAddress\":\"192.168.65.3\",\"details\":{\"auth_method\":\"openid-connect\",\"auth_type\":\"code\",\"register_method\":\"form\",\"redirect_uri\":\"http://localhost:8080/realms/pridr/account?referrer=Frontend&referrer_uri=http%3A%2F%2Flocalhost%3A5173%2Fprofile\",\"code_id\":\"ceb07c03-89fc-4ee2-b2c5-c7a477ce3535\",\"email\":\"testjebla@blabla.com\",\"username\":\"testjebla@blabla.com\"}}";
            
            // Act 
            _mockMessagePublisher.PublishMessage(
                message, 
                _sharedSetup.ContainerSetup.RabbitMqContainer.Hostname, 
                _sharedSetup.ContainerSetup.RabbitMqContainer.GetMappedPublicPort(5672),
                "DeleteUser"
            );
            
            // Assert
            bool isProcessed = false;
            await WaitForConditionAsync(
                async () => isProcessed = await CheckIfUserIsDeleted("a6427685-84e3-4fbf-8716-c94d1053b020"),
                TimeSpan.FromSeconds(60),
                TimeSpan.FromMilliseconds(500)
            );
            Assert.True(isProcessed, "Het bericht is niet correct verwerkt. User is niet verwijderd uit alle DB's");
        }
    }

    private static async Task WaitForConditionAsync(Func<Task<bool>> condition, TimeSpan timeout, TimeSpan pollingInterval)
    {
        var start = DateTime.UtcNow;

        while (DateTime.UtcNow - start < timeout)
        {
            if (await condition())
            {
                Console.WriteLine("Condition met, proceeding...");
                return;
            }
            await Task.Delay(pollingInterval);
        }
        throw new TimeoutException("Condition was not met within the timeout period.");
    }
    
    private async Task<bool> CheckIfUserExists(string keyCloakId)
    {
        try
        {
            // userservice check
            await using var userServiceConnection = new NpgsqlConnection(_sharedSetup.ContainerSetup.userServiceConnString);
            await userServiceConnection.OpenAsync();
            
            const string userServiceSql = "SELECT EXISTS (SELECT 1 FROM public.\"Profiles\" WHERE \"KeyCloakId\" = @KeyCloakId);";
            await using var userServiceCommand = new NpgsqlCommand(userServiceSql, userServiceConnection);
            userServiceCommand.Parameters.AddWithValue("@KeyCloakId", keyCloakId);
            
            var userServiceResult = await userServiceCommand.ExecuteScalarAsync();
            
            
            // chatservice check
            await using var chatServiceConnection = new NpgsqlConnection(_sharedSetup.ContainerSetup.chatServiceConnString);
            await chatServiceConnection.OpenAsync();
            
            const string chatServiceSql = "SELECT EXISTS (SELECT 1 FROM public.\"Usernames\" WHERE \"KeycloakId\" = @KeycloakId);";
            await using var chatServiceCommand = new NpgsqlCommand(chatServiceSql, chatServiceConnection);
            chatServiceCommand.Parameters.AddWithValue("@KeycloakId", keyCloakId);
            
            var chatServiceResult = await chatServiceCommand.ExecuteScalarAsync();

            return userServiceResult is bool userServiceAdded && userServiceAdded &&
                   chatServiceResult is bool chatServiceAdded && chatServiceAdded;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking user existence: {ex.Message}");
            return false;
        }
    }
    
    private async Task<bool> CheckIfUsernameIsUpdated(string keyCloakId, string newUserName)
    {
        try
        {
            // userservice check
            await using var userServiceConnection = new NpgsqlConnection(_sharedSetup.ContainerSetup.userServiceConnString);
            await userServiceConnection.OpenAsync();

            const string userServiceSql = "SELECT \"UserName\" FROM public.\"Profiles\" WHERE \"KeyCloakId\" = @KeyCloakId;";
            await using var userServiceCommand = new NpgsqlCommand(userServiceSql, userServiceConnection);
            userServiceCommand.Parameters.AddWithValue("@KeyCloakId", keyCloakId);

            var userServiceResult = await userServiceCommand.ExecuteScalarAsync();

            if (userServiceResult is string username)
            {
                // Decrypt the username here if needed
                if (newUserName == EncryptionHelper.Decrypt(username))
                {
                    // Continue to check the chatservice
                    await using var chatServiceConnection = new NpgsqlConnection(_sharedSetup.ContainerSetup.chatServiceConnString);
                    await chatServiceConnection.OpenAsync();

                    const string chatServiceSql = "SELECT \"UserName\" FROM public.\"Usernames\" WHERE \"KeycloakId\" = @KeycloakId;";
                    await using var chatServiceCommand = new NpgsqlCommand(chatServiceSql, chatServiceConnection);
                    chatServiceCommand.Parameters.AddWithValue("@KeycloakId", keyCloakId);

                    var chatServiceResult = await chatServiceCommand.ExecuteScalarAsync();

                    if (chatServiceResult is string usernameChatService)
                    {
                        // Decrypt the username here if needed
                        if (newUserName == EncryptionHelper.Decrypt(usernameChatService))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking if username is updated: {ex.Message}");
            return false;
        }
    }
    
    private async Task<bool> CheckIfUserEventExists(string keyCloakId)
    {
        try
        {
            await using var eventServiceConnection = new NpgsqlConnection(_sharedSetup.ContainerSetup.eventServiceConnString);
            await eventServiceConnection.OpenAsync();

            const string eventServiceSql = "SELECT EXISTS (SELECT 1 FROM public.\"UserEvents\" WHERE \"CreatedBy\" = @KeyCloakId);";
            await using var eventServiceCommand = new NpgsqlCommand(eventServiceSql, eventServiceConnection);
            eventServiceCommand.Parameters.AddWithValue("@KeyCloakId", keyCloakId);
            
            var eventServiceResult = await eventServiceCommand.ExecuteScalarAsync();
            return eventServiceResult is bool userEventAdded && userEventAdded;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking user event existence: {ex.Message}");
            return false;
        }
    }
    
    private async Task<bool> CheckIfUserIsDeleted(string keyCloakId)
    {
    try
    {
        // userservice check
        await using var userServiceConnection = new NpgsqlConnection(_sharedSetup.ContainerSetup.userServiceConnString);
        await userServiceConnection.OpenAsync();

        const string userServiceSql = "SELECT EXISTS (SELECT 1 FROM public.\"Profiles\" WHERE \"KeyCloakId\" = @KeyCloakId);";
        await using var userServiceCommand = new NpgsqlCommand(userServiceSql, userServiceConnection);
        userServiceCommand.Parameters.AddWithValue("@KeyCloakId", keyCloakId);

        var userServiceResult = await userServiceCommand.ExecuteScalarAsync();
        var userDeleted = userServiceResult is bool boolUserDeleted && !boolUserDeleted;

        // chatservice check
        await using var chatServiceConnection = new NpgsqlConnection(_sharedSetup.ContainerSetup.chatServiceConnString);
        await chatServiceConnection.OpenAsync();

        const string chatServiceSql = "SELECT \"UserName\" FROM public.\"Usernames\" WHERE \"KeycloakId\" = @KeycloakId;";
        await using var chatServiceCommand = new NpgsqlCommand(chatServiceSql, chatServiceConnection);
        chatServiceCommand.Parameters.AddWithValue("@KeycloakId", keyCloakId);

        var chatServiceResult = await chatServiceCommand.ExecuteScalarAsync();
        var usernameUpdated = chatServiceResult is string username && "Deleted User" == EncryptionHelper.Decrypt(username);

        // eventservice check
        await using var eventServiceConnection = new NpgsqlConnection(_sharedSetup.ContainerSetup.eventServiceConnString);
        await eventServiceConnection.OpenAsync();

        const string eventServiceSql = "SELECT EXISTS (SELECT 1 FROM public.\"UserEvents\" WHERE \"CreatedBy\" = @KeyCloakId);";
        await using var eventServiceCommand = new NpgsqlCommand(eventServiceSql, eventServiceConnection);
        eventServiceCommand.Parameters.AddWithValue("@KeyCloakId", keyCloakId);

        var eventServiceResult = await eventServiceCommand.ExecuteScalarAsync();
        var eventDeleted = eventServiceResult is bool boolEventDeleted && !boolEventDeleted;

        return userDeleted && usernameUpdated && eventDeleted;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error checking if user is deleted everywhere: {ex.Message}");
        return false;
    }
    }
}
