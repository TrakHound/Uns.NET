<img src="img/uns-net-logo-text-01.png" style="width: 350px;">


# Overview
.NET SDK for implementing a Unified Namespace for use with IIOT. Supports plain MQTT and SparkplugB.

- Consolidate multiple connections (Plain MQTT, SparkplugB, etc.)
- Subscribe & Publish with SparkplugB using standardized Paths
- Apply Namespace configuration based on Path
- Implement Middleware such as Report By Exception, Unit Conversion, etc.

# Nuget
```
dotnet add package Uns.NET --version 0.1.0-beta
```

# UnsClient
The UnsClient class is the primary class that handles Connections, Namespace Configurations, and Subscriptions. The UnsClient allows an application to Subscribe and Publish data in the form of a standardized **UnsEventMessage**. A UnsEventMessage contains the content of the message along with information about the Namespace and Connection.

## UnsEventMessage

```
Path = Plant1/Area3/Line4/Cell2/PLC/PLC-01/Temperature
Namespace.Path = Plant1/Area3/Line4/Cell2/PLC
Namespace.Type = Functional
Namespace.Kind = Heterogenous
Connection.Id = spB
Connection.Type = SPARKPLUG_B_APPLICATION
ContentType = SPARKPLUG_B_METRIC
Content = {"Name":"Temperature","Timestamp":1708139619918,"BytesValue":"","Value":514,"ValueCase":9,"DataType":9}
```

## Usage
```c#
using Uns;

var client = new UnsClient();

// Add Middleware
client.AddMiddleware(new UnsReportByExceptionMiddleware());
client.AddMiddleware(new UnsDeadbandPeriodMiddleware(TimeSpan.FromSeconds(5)));
client.AddMiddleware(new UnsDeadbandValueMiddleware(10));

// Add Namespace Configurations
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
consumer.Received += EventReceived;

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

    await client.Publish("Plant1/Area3/Line4/Cell2/PLC/PLC-01/Temperature", 100.32);
    await client.Publish("Plant1/Area3/Line4/Cell2/PLC/PLC-02/Text", "THIS IS FROM A UNS CLIENT");
}

await client.Stop();


async void EventReceived(object? sender, UnsEventMessage message)
{
    Console.WriteLine("######################");
    Console.WriteLine($"Path = {message.Path}");
    Console.WriteLine($"Namespace = {message.Namespace?.Path}");
    Console.WriteLine($"Connection = {message.Connection?.Id}");
    Console.WriteLine($"ContentType = {message.ContentType}");
    Console.WriteLine($"Content = {GetContentString(message.ContentType, message.Content)}");
    Console.WriteLine($"Timestamp = {message.Timestamp.ToString("o")}");

    await client.Publish("mqtt-01", message);
}

string GetContentString(NamespaceContentType contentType, byte[] content)
{
    switch (contentType)
    {
        case NamespaceContentType.PlainText: return System.Text.Encoding.UTF8.GetString(content);
        case NamespaceContentType.Json: return System.Text.Encoding.UTF8.GetString(content);
        case NamespaceContentType.SparkplugB: return System.Text.Encoding.UTF8.GetString(content);
    }

    return null;
}
```

## Connections
**UnsConnections** are used to connect to external systems and either subscribe to or publish data in the form of **UnsEvents**.

### Connection Interfaces
- `IUnsInputConnection` : Only allow to subscribe to data (Read-Only)
- `IUnsOutputConnection` : Only allow to publish new data (Write-Only)

### UnsMqttConnection
```c#
var mqttConnection = new UnsMqttConnection("localhost", 1883);
mqttConnection.AddSubscription("input/ERP/#");
mqttConnection.AddSubscription("input/MES/#");
mqttConnection.AddDestination("Plant1/ERP");
mqttConnection.AddDestination("Plant1/Area3/Line4/MES");
client.AddConnection(mqttConnection);
```

### UnsSparkplugConnection


#### AddApplication
```c#
var spBConnection = new UnsSparkplugConnection("localhost", 1883);
spBConnection.AddApplication("Plant1/Area3/Line4/Cell2");
client.AddConnection(spBConnection);
```

#### AddNode
```c#
var spBConnection = new UnsSparkplugConnection("localhost", 1883);
spBConnection.AddNode("Plant1/Area3/Line4/Cell2/PLC");
client.AddConnection(spBConnection);
```

#### AddDevice
```c#
var spBConnection = new UnsSparkplugConnection("localhost", 1883);
spBConnection.AddDevice("Plant1/Area3/Line4/Cell2/PLC/PLC-01");
client.AddConnection(spBConnection);
```

## Namespace Configuration
Namespace Configurations are used to define the Namespace that match events. This can be used to filter events or to send as information to an external application. Configurations can be either manually set as shown below or be populated from an external Broker, API, or Database.
```c#
var siteNamespaceConfig = new NamespaceConfiguration();
siteNamespaceConfig.Path = "Plant1";
siteNamespaceConfig.Kind = NamespaceKind.Homogeneous;
siteNamespaceConfig.Type = NamespaceType.Informational;
client.AddNamespace(siteNamespaceConfig);

var plcNamespaceConfig = new NamespaceConfiguration();
plcNamespaceConfig.Path = "Plant1/Area3/Line4/Cell2/PLC";
plcNamespaceConfig.Kind = NamespaceKind.Heterogenous;
plcNamespaceConfig.Type = NamespaceType.Functional;
plcNamespaceConfig.ContentType = NamespaceContentType.SPARKPLUG_B;
client.AddNamespace(plcNamespaceConfig);
```

## Middleware
Middleware is used to filter, transform, etc. events that are either sent or received.

### UnsReportByExceptionMiddleware
Report by Exception (RBE) is a fundamental principle of a Unified Namespace as it reduces the amount of data sent over the network by filtering out duplicate data.
```c#
// Add RBE Middleware to a UnsClient
client.AddMiddleware(new UnsReportByExceptionMiddleware());
```

### UnsDeadbandValueMiddleware
A Value Deadband filter is used to filter out values that havent' changed by the specified MinimumDelta. This can be used to filter out "noise" as well as reduce the amount of data sent that may be negligible.
```c#
// Add a Value Deadband filter to a UnsClient to filter
// requests whose value hasn't changed by more than +/- 10
client.AddMiddleware(new UnsDeadbandValueMiddleware(10));
```

### UnsDeadbandPeriodMiddleware
A Period Deadband filter is used to filter out values that havent' changed within the specified MinimumPeriod time span. This can be used to filter out "noise" as well as reduce the amount of data sent that may be negligible.
```c#
// Add a Period Deadband filter to a UnsClient to filter
// requests whose value hasn't changed in the last 5 seconds
client.AddMiddleware(new UnsDeadbandPeriodMiddleware(TimeSpan.FromSeconds(5)));
```
