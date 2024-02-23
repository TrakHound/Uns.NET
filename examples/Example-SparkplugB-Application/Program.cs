using Uns;


// Declare a new UnsClient
var client = new UnsClient();


// Add Sparkplug Connection
var spBConnection = new UnsSparkplugConnection("localhost", 1883, "testing");
spBConnection.AddApplication();
client.AddConnection(spBConnection);


// Subscribe to any Path
var consumer = client.Subscribe("Site/#");
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