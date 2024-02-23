using Uns;


// Declare a new UnsClient
var client = new UnsClient();


// Add RBE Middleware
client.AddMiddleware(new UnsReportByExceptionMiddleware());


// Add MQTT (Plain) Connection
var mqttConnection = new UnsMqttConnection("localhost", 1883);
mqttConnection.AddSubscription("#");
client.AddConnection(mqttConnection);


// Subscribe to any Path
var consumer = client.Subscribe("#");
consumer.Received += (c, o) =>
{
    Console.WriteLine("-------------------------");
    Console.WriteLine($"Path = {o.Path}");
    Console.WriteLine($"Connection.Id = {o.Connection?.Id}");
    Console.WriteLine($"Connection.Type = {o.Connection?.Type}");
    Console.WriteLine($"ContentType = {o.ContentType}");
    Console.WriteLine($"Content = {System.Text.Encoding.UTF8.GetString(o.Content)}");
    Console.WriteLine($"Timestamp = {o.Timestamp.ToString("o")}");
};

// Start Client
await client.Start();

Console.WriteLine("Press a key to stop..");
Console.ReadLine();

// Stop Client
await client.Stop();