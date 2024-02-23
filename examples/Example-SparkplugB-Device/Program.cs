using Uns;


// Declare a new UnsClient
var client = new UnsClient();


// Add Sparkplug Connection
var spBConnection = new UnsSparkplugConnection("localhost", 1883, "testing");
spBConnection.AddDevice("Site/Cell/PLC");
client.AddConnection(spBConnection);

// Start Client
await client.Start();

while (true)
{
    Console.WriteLine("Enter Event Path (ex. Site/Cell/PLC/Temperature)..");
    var eventPath = Console.ReadLine();

    Console.WriteLine("Enter Event Content (ex. 45.2)..");
    var eventContent = Console.ReadLine();

    await client.Publish(eventPath, eventContent);
}
