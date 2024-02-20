using System.Text;
using Uns;

var client = new UnsClient();

var erpNamespaceConfig = new NamespaceConfiguration();
erpNamespaceConfig.Path = "Plant1/ERP";
erpNamespaceConfig.Kind = NamespaceKind.Homogeneous;
erpNamespaceConfig.Type = NamespaceType.Informational;
client.AddNamespace(erpNamespaceConfig);

var mesNamespaceConfig = new NamespaceConfiguration();
mesNamespaceConfig.Path = "Plant1/Area3/Line4/MES";
mesNamespaceConfig.Kind = NamespaceKind.Heterogenous;
mesNamespaceConfig.Type = NamespaceType.Functional;
client.AddNamespace(mesNamespaceConfig);

var plcNamespaceConfig = new NamespaceConfiguration();
plcNamespaceConfig.Path = "Plant1/Area3/Line4/Cell2/PLC";
plcNamespaceConfig.Kind = NamespaceKind.Heterogenous;
plcNamespaceConfig.Type = NamespaceType.Functional;
client.AddNamespace(plcNamespaceConfig);


// Add MQTT (Plain) Connection
var mqttConnection = new UnsMqttConnection("localhost", 1883);
mqttConnection.AddSubscription("input/ERP/#", erpNamespaceConfig.Path);
mqttConnection.AddSubscription("input/MES/#", mesNamespaceConfig.Path);
mqttConnection.AddDestination(erpNamespaceConfig.Path);
mqttConnection.AddDestination(mesNamespaceConfig.Path);
client.AddConnection(mqttConnection);


// Add Sparkplug Connection
var spBConnection = new UnsSparkplugConnection("localhost", 1883, "testing");
spBConnection.AddApplication("Plant1/Area3/Line4/Cell2/PLC");
spBConnection.AddNode("Plant1/Area3/Line4/Cell2/PLC");
spBConnection.AddDevice("Plant1/Area3/Line4/Cell2/PLC/PLC-01");
spBConnection.AddDevice("Plant1/Area3/Line4/Cell2/PLC/PLC-02");
client.AddConnection(spBConnection);


var consumer = client.Subscribe("#");
consumer.Received += async (c, o) =>
{
    Console.WriteLine("-------------------------");
    Console.WriteLine($"Path = {o.Path}");
    Console.WriteLine($"Namespace.Path = {o.Namespace?.Path}");
    Console.WriteLine($"Namespace.Type = {o.Namespace?.Type}");
    Console.WriteLine($"Namespace.Kind = {o.Namespace?.Kind}");
    Console.WriteLine($"Connection.Id = {o.Connection?.Id}");
    Console.WriteLine($"Connection.Type = {o.Connection?.Type}");
    Console.WriteLine($"ContentType = {o.ContentType}");
    Console.WriteLine($"Content = {GetContentString(o.ContentType, o.Content)}");
    Console.WriteLine($"Timestamp = {o.Timestamp.ToString("o")}");

    await client.Publish(o);
};

var temperatureConsumer = client.Subscribe<double>("Temperature");
temperatureConsumer.Received += (c, o) => Console.WriteLine(o.ToJson(true));

var descriptionConsumer = client.Subscribe<string>("Description");
descriptionConsumer.Received += (c, o) => Console.WriteLine(o.ToJson(true));

var modelConsumer = client.SubscribeJson<Model>("#");
modelConsumer.Received += (c, o) => Console.WriteLine(o.ToJson(true));

await client.Start();

while (true)
{
    Console.ReadLine();

    await client.Publish("Plant1/Area3/Line4/Cell2/PLC/PLC-02/Temperature", 100.32);
    await client.Publish("Plant1/Area3/Line4/Cell2/PLC/PLC-02/Text", "THIS IS FROM A UNS CLIENT");
}


await client.Stop();


string GetContentString(UnsEventContentType contentType, byte[] content)
{
    switch (contentType)
    {
        case UnsEventContentType.STRING: return Encoding.UTF8.GetString(content);
        case UnsEventContentType.JSON: return Encoding.UTF8.GetString(content);
        case UnsEventContentType.SPARKPLUG_B_METRIC: return Encoding.UTF8.GetString(content);
    }

    return null;
}


class Model
{
    public string Name { get; set; }
    public string Value { get; set; }
    public long Timestamp { get; set; }
}
