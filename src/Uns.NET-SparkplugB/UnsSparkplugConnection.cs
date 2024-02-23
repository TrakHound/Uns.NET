// Copyright (c) 2024 TrakHound Inc., All Rights Reserved.
// TrakHound Inc. licenses this file to you under the MIT license.

using SparkplugNet.Core;
using SparkplugNet.Core.Application;
using SparkplugNet.Core.Node;
using SparkplugNet.VersionB;
using SparkplugNet.VersionB.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Uns.Extensions;

namespace Uns
{
    public class UnsSparkplugConnection : UnsConnectionBase
    {
        private const string _type = "SPARKPLUG_B";
        private const int _defaultReconnectInterval = 30000; // 30 Seconds

        private readonly string _id;
        private readonly UnsMqttConnectionConfiguration _configuration;
        private readonly Dictionary<string, SparkplugApplication> _applications = new Dictionary<string, SparkplugApplication>();
        private readonly Dictionary<string, SparkplugApplicationOptions> _applicationOptions = new Dictionary<string, SparkplugApplicationOptions>();
        private readonly Dictionary<string, SparkplugNode> _nodes = new Dictionary<string, SparkplugNode>();
        private readonly Dictionary<string, SparkplugNode> _devices = new Dictionary<string, SparkplugNode>();
        private readonly Dictionary<string, SparkplugNodeOptions> _nodeOptions = new Dictionary<string, SparkplugNodeOptions>();
        private readonly ListDictionary<string, Metric> _metrics = new ListDictionary<string, Metric>();


        public override string Type => _type;

        public override string Id => _id;


        public UnsSparkplugConnection(string server, int port = 1883, string clientId = null)
        {
            _id = Guid.NewGuid().ToString();

            var configuration = new UnsMqttConnectionConfiguration();
            configuration.Server = server;
            configuration.Port = port;
            configuration.ClientId = clientId;
            _configuration = configuration;
        }

        public UnsSparkplugConnection(UnsMqttConnectionConfiguration configuration)
        {
            _id = Guid.NewGuid().ToString();
            _configuration = configuration;
        }


        //public void AddApplication(string groupId = null, string nodeId = null, string deviceId = null)
        //{
        //    var applicationOptions = new SparkplugApplicationOptions(
        //        brokerAddress: _configuration.Server,
        //        port: _configuration.Port,
        //        clientId: _configuration.ClientId,
        //        userName: _configuration.Username,
        //        password: _configuration.Password,
        //        useTls: _configuration.UseTls,
        //        reconnectInterval: TimeSpan.FromMilliseconds(_defaultReconnectInterval)
        //        );

        //    var application = new SparkplugApplication(_metrics.Values);
        //    application.ConnectedAsync += OnConnected;
        //    application.DisconnectedAsync += OnDisonnected;
        //    application.NodeDataReceivedAsync += (o) => NodeDataReceivedAsync(groupId, nodeId, o);
        //    application.NodeBirthReceivedAsync += (o) => NodeBirthReceivedAsync(groupId, nodeId, o);
        //    application.DeviceBirthReceivedAsync += (o) => DeviceBirthReceivedAsync(groupId, nodeId, deviceId, o);
        //    application.DeviceDataReceivedAsync += (o) => DeviceDataReceivedAsync(groupId, nodeId, deviceId, o);

        //    var applicationKey = $"{groupId}:{nodeId}:{deviceId}";

        //    _applicationOptions.Remove(applicationKey);
        //    _applicationOptions.Add(applicationKey, applicationOptions);

        //    _applications.Remove(applicationKey);
        //    _applications.Add(applicationKey, application);
        //}

        public void AddApplication(string path = "/")
        {
            if (!string.IsNullOrEmpty(path))
            {
                var applicationOptions = new SparkplugApplicationOptions(
                    brokerAddress: _configuration.Server,
                    port: _configuration.Port,
                    clientId: _configuration.ClientId,
                    userName: _configuration.Username,
                    password: _configuration.Password,
                    useTls: _configuration.UseTls,
                    reconnectInterval: TimeSpan.FromMilliseconds(_defaultReconnectInterval)
                    );

                var application = new SparkplugApplication(_metrics.Values);
                application.ConnectedAsync += OnConnected;
                application.DisconnectedAsync += OnDisonnected;
                application.NodeDataReceivedAsync += (o) => NodeDataReceivedAsync(path, o);
                application.NodeBirthReceivedAsync += (o) => NodeBirthReceivedAsync(path, o);
                application.DeviceBirthReceivedAsync += (o) => DeviceBirthReceivedAsync(path, o);
                application.DeviceDataReceivedAsync += (o) => DeviceDataReceivedAsync(path, o);

                _applicationOptions.Remove(path);
                _applicationOptions.Add(path, applicationOptions);

                _applications.Remove(path);
                _applications.Add(path, application);
            }
        }

        public void AddNode(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                var groupId = UnsPath.GetParentPath(path).Replace('/', ':');
                var nodeId = UnsPath.GetObject(path);

                var nodeOptions = new SparkplugNodeOptions(
                    brokerAddress: _configuration.Server,
                    port: _configuration.Port,
                    clientId: _configuration.ClientId,
                    userName: _configuration.Username,
                    password: _configuration.Password,
                    useTls: _configuration.UseTls,
                    groupIdentifier: groupId,
                    edgeNodeIdentifier: nodeId,
                    reconnectInterval: TimeSpan.FromMilliseconds(_defaultReconnectInterval)
                    );

                var node = new SparkplugNode(_metrics.Values);
                node.ConnectedAsync += OnConnected;
                node.DisconnectedAsync += OnDisonnected;

                _nodeOptions.Remove(path);
                _nodeOptions.Add(path, nodeOptions);

                _nodes.Remove(path);
                _nodes.Add(path, node);
            }
        }

        public void AddDevice(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                var nodePath = UnsPath.GetParentPath(path);
                var groupId = UnsPath.GetParentPath(nodePath).Replace('/', ':');
                var nodeId = UnsPath.GetObject(nodePath);

                var nodeOptions = new SparkplugNodeOptions(
                    brokerAddress: _configuration.Server,
                    port: _configuration.Port,
                    clientId: _configuration.ClientId,
                    userName: _configuration.Username,
                    password: _configuration.Password,
                    useTls: _configuration.UseTls,
                    groupIdentifier: groupId,
                    edgeNodeIdentifier: nodeId,
                    reconnectInterval: TimeSpan.FromMilliseconds(_defaultReconnectInterval)
                    );

                var node = new SparkplugNode(_metrics.Values);
                node.ConnectedAsync += OnConnected;
                node.DisconnectedAsync += OnDisonnected;

                _nodeOptions.Remove(path);
                _nodeOptions.Add(path, nodeOptions);

                _devices.Remove(path);
                _devices.Add(path, node);
            }
        }



        private Task OnConnected(SparkplugBase<Metric>.SparkplugEventArgs args)
        {
            UpdateConnectionStatus(UnsConnectionStatus.Connected);
            return Task.CompletedTask;
        }

        private Task OnDisonnected(SparkplugBase<Metric>.SparkplugEventArgs args)
        {
            UpdateConnectionStatus(UnsConnectionStatus.Disconnected);
            return Task.CompletedTask;
        }


        private Task NodeBirthReceivedAsync(string basePath, SparkplugBase<Metric>.NodeBirthEventArgs args)
        {
            if (args.Metrics.Count() > 1)
            {
                foreach (var metric in args.Metrics)
                {
                    if (metric.Name != "bdSeq")
                    {
                        var path = $"{args.GroupIdentifier}/{args.EdgeNodeIdentifier}/{metric.Name}".Replace(':', '/');

                        if (UnsPath.IsChildOf(basePath, path))
                        {
                            var message = new UnsEventMessage();
                            message.Path = path;
                            message.ContentType = UnsEventContentType.SPARKPLUG_B_METRIC;
                            message.Content = System.Text.Encoding.UTF8.GetBytes(Json.Convert(metric));
                            message.Timestamp = SparkplugTimestamp.ToDateTime(metric.Timestamp);

                            ReceiveEvent(message);
                        }
                    }
                }
            }

            return Task.CompletedTask;
        }

        private Task NodeDataReceivedAsync(string basePath, SparkplugApplicationBase<Metric>.NodeDataEventArgs args)
        {
            if (args.Metric.Name != "bdSeq")
            {
                var path = $"{args.GroupIdentifier}/{args.EdgeNodeIdentifier}/{args.Metric.Name}".Replace(':', '/');

                if (UnsPath.IsChildOf(basePath, path))
                {
                    var message = new UnsEventMessage();
                    message.Path = path;
                    message.ContentType = UnsEventContentType.SPARKPLUG_B_METRIC;
                    message.Content = System.Text.Encoding.UTF8.GetBytes(Json.Convert(args.Metric));
                    message.Timestamp = SparkplugTimestamp.ToDateTime(args.Metric.Timestamp);

                    ReceiveEvent(message);
                }
            }

            return Task.CompletedTask;
        }

        private Task DeviceBirthReceivedAsync(string basePath, SparkplugBase<Metric>.DeviceBirthEventArgs args)
        {
            if (args.Metrics.Count() > 1)
            {
                foreach (var metric in args.Metrics)
                {
                    if (metric.Name != "bdSeq")
                    {
                        var path = $"{args.GroupIdentifier}/{args.EdgeNodeIdentifier}/{args.DeviceIdentifier}/{metric.Name}".Replace(':', '/');

                        if (UnsPath.IsChildOf(basePath, path))
                        {
                            var message = new UnsEventMessage();
                            message.Path = path;
                            message.ContentType = UnsEventContentType.SPARKPLUG_B_METRIC;
                            message.Content = System.Text.Encoding.UTF8.GetBytes(Json.Convert(metric));
                            message.Timestamp = SparkplugTimestamp.ToDateTime(metric.Timestamp);

                            ReceiveEvent(message);
                        }
                    }
                }
            }

            return Task.CompletedTask;
        }

        private Task DeviceDataReceivedAsync(string basePath, SparkplugApplicationBase<Metric>.DeviceDataEventArgs args)
        {
            if (args.Metric.Name != "bdSeq")
            {
                var path = $"{args.GroupIdentifier}/{args.EdgeNodeIdentifier}/{args.DeviceIdentifier}/{args.Metric.Name}".Replace(':', '/');

                if (UnsPath.IsChildOf(basePath, path))
                {
                    var message = new UnsEventMessage();
                    message.Path = path;
                    message.ContentType = UnsEventContentType.SPARKPLUG_B_METRIC;
                    message.Content = System.Text.Encoding.UTF8.GetBytes(Json.Convert(args.Metric));
                    message.Timestamp = SparkplugTimestamp.ToDateTime(args.Metric.Timestamp);

                    ReceiveEvent(message);
                }
            }

            return Task.CompletedTask;
        }

        //private Task NodeBirthReceivedAsync(string groupId, string nodeId, SparkplugBase<Metric>.NodeBirthEventArgs args)
        //{
        //    if (args.Metrics.Count() > 1)
        //    {
        //        foreach (var metric in args.Metrics)
        //        {
        //            if (metric.Name != "bdSeq")
        //            {
        //                var path = $"{args.GroupIdentifier}/{args.EdgeNodeIdentifier}/{metric.Name}".Replace(':', '/');

        //                var match = groupId == null || groupId == args.GroupIdentifier;
        //                if (match) match = nodeId == null || nodeId == args.EdgeNodeIdentifier;

        //                if (match)
        //                {
        //                    var message = new UnsEventMessage();
        //                    message.Path = path;
        //                    message.ContentType = UnsEventContentType.SPARKPLUG_B_METRIC;
        //                    message.Content = System.Text.Encoding.UTF8.GetBytes(Json.Convert(metric));
        //                    message.Timestamp = SparkplugTimestamp.ToDateTime(metric.Timestamp);

        //                    ReceiveEvent(message);
        //                }
        //            }
        //        }
        //    }

        //    return Task.CompletedTask;
        //}

        //private Task NodeDataReceivedAsync(string groupId, string nodeId, SparkplugApplicationBase<Metric>.NodeDataEventArgs args)
        //{
        //    if (args.Metric.Name != "bdSeq")
        //    {
        //        var path = $"{args.GroupIdentifier}/{args.EdgeNodeIdentifier}/{args.Metric.Name}".Replace(':', '/');

        //        var match = groupId == null || groupId == args.GroupIdentifier;
        //        if (match) match = nodeId == null || nodeId == args.EdgeNodeIdentifier;

        //        if (match)
        //        {
        //            var message = new UnsEventMessage();
        //            message.Path = path;
        //            message.ContentType = UnsEventContentType.SPARKPLUG_B_METRIC;
        //            message.Content = System.Text.Encoding.UTF8.GetBytes(Json.Convert(args.Metric));
        //            message.Timestamp = SparkplugTimestamp.ToDateTime(args.Metric.Timestamp);

        //            ReceiveEvent(message);
        //        }
        //    }

        //    return Task.CompletedTask;
        //}

        //private Task DeviceBirthReceivedAsync(string groupId, string nodeId, string deviceId, SparkplugBase<Metric>.DeviceBirthEventArgs args)
        //{
        //    if (args.Metrics.Count() > 1)
        //    {
        //        foreach (var metric in args.Metrics)
        //        {
        //            if (metric.Name != "bdSeq")
        //            {
        //                var path = $"{args.GroupIdentifier}/{args.EdgeNodeIdentifier}/{args.DeviceIdentifier}/{metric.Name}".Replace(':', '/');

        //                var match = groupId == null || groupId == args.GroupIdentifier;
        //                if (match) match = nodeId == null || nodeId == args.EdgeNodeIdentifier;
        //                if (match) match = deviceId == null || deviceId == args.DeviceIdentifier;

        //                if (match)
        //                {
        //                    var message = new UnsEventMessage();
        //                    message.Path = path;
        //                    message.ContentType = UnsEventContentType.SPARKPLUG_B_METRIC;
        //                    message.Content = System.Text.Encoding.UTF8.GetBytes(Json.Convert(metric));
        //                    message.Timestamp = SparkplugTimestamp.ToDateTime(metric.Timestamp);

        //                    ReceiveEvent(message);
        //                }
        //            }
        //        }
        //    }

        //    return Task.CompletedTask;
        //}

        //private Task DeviceDataReceivedAsync(string groupId, string nodeId, string deviceId, SparkplugApplicationBase<Metric>.DeviceDataEventArgs args)
        //{
        //    if (args.Metric.Name != "bdSeq")
        //    {
        //        var path = $"{args.GroupIdentifier}/{args.EdgeNodeIdentifier}/{args.DeviceIdentifier}/{args.Metric.Name}".Replace(':', '/');

        //        var match = groupId == null || groupId == args.GroupIdentifier;
        //        if (match) match = nodeId == null || nodeId == args.EdgeNodeIdentifier;
        //        if (match) match = deviceId == null || deviceId == args.DeviceIdentifier;

        //        if (match)
        //        {
        //            var message = new UnsEventMessage();
        //            message.Path = path;
        //            message.ContentType = UnsEventContentType.SPARKPLUG_B_METRIC;
        //            message.Content = System.Text.Encoding.UTF8.GetBytes(Json.Convert(args.Metric));
        //            message.Timestamp = SparkplugTimestamp.ToDateTime(args.Metric.Timestamp);

        //            ReceiveEvent(message);
        //        }
        //    }

        //    return Task.CompletedTask;
        //}


        public override async Task Start()
        {
            // Start Applications
            foreach (var key in _applications.Keys)
            {
                var applicationOptions = _applicationOptions.GetValueOrDefault(key);
                var application = _applications.GetValueOrDefault(key);

                if (application != null && applicationOptions != null)
                {
                    await application.Start(applicationOptions);
                }
            }

            // Start Nodes
            foreach (var key in _nodes.Keys)
            {
                var nodeOptions = _nodeOptions.GetValueOrDefault(key);
                var node = _nodes.GetValueOrDefault(key);

                if (node != null && nodeOptions != null)
                {
                    await node.Start(nodeOptions);
                }
            }

            // Start Devices
            foreach (var key in _devices.Keys)
            {
                var nodeOptions = _nodeOptions.GetValueOrDefault(key);
                var device = _devices.GetValueOrDefault(key);

                if (device != null && nodeOptions != null)
                {
                    var deviceId = UnsPath.GetObject(key);

                    await device.Start(nodeOptions);
                    await device.PublishDeviceBirthMessage(new List<Metric>(_metrics.Values), deviceId);
                }
            }
        }

        public override async Task Stop()
        {
            // Stop Applications
            foreach (var key in _applications.Keys)
            {
                var applicationOptions = _applicationOptions.GetValueOrDefault(key);
                var application = _applications.GetValueOrDefault(key);

                if (application != null && applicationOptions != null)
                {
                    await application.Stop();
                }
            }

            // Stop Nodes
            foreach (var key in _nodes.Keys)
            {
                var nodeOptions = _nodeOptions.GetValueOrDefault(key);
                var node = _nodes.GetValueOrDefault(key);

                if (node != null && nodeOptions != null)
                {
                    await node.Stop();
                }
            }

            // Stop Devices
            foreach (var key in _devices.Keys)
            {
                var nodeOptions = _nodeOptions.GetValueOrDefault(key);
                var device = _devices.GetValueOrDefault(key);

                if (device != null && nodeOptions != null)
                {
                    await device.Stop();
                }
            }
        }



        public override async Task Publish(UnsEventMessage message)
        {
            if (message != null && !string.IsNullOrEmpty(message.Path))
            {
                var found = false;

                // Publish Devices
                var paths = _devices.Keys.OrderByDescending(o => o);
                foreach (var path in _devices.Keys)
                {
                    if (UnsPath.IsChildOf(path, message.Path))
                    {
                        var deviceId = UnsPath.GetObject(path);

                        var name = UnsPath.GetRelativeTo(path, message.Path);
                        var dataType = GetDataType(message.ContentType);
                        var value = System.Text.Encoding.UTF8.GetString(message.Content);

                        var metric = new Metric(name, dataType, value, message.Timestamp);

                        var device = _devices.GetValueOrDefault(path);
                        if (device != null)
                        {
                            // Update Metrics
                            if (!_metrics.ContainsKey(path))
                            {
                                _metrics.Remove(path);
                                _metrics.Add(path, metric);

                                device.KnownDevices.TryRemove(deviceId, out _);
                                device.KnownDevices.TryAdd(deviceId, new SparkplugBase<Metric>.KnownMetricStorage(_metrics.Values));

                                await device.PublishDeviceBirthMessage(new List<Metric>() { metric }, deviceId);
                            }
                            else
                            {
                                var metrics = _metrics.Get(path);
                                if (!metrics.Any(o => o.Name == name))
                                {
                                    _metrics.Add(path, metric);
                                    metrics = _metrics.Get(path);

                                    device.KnownDevices.TryRemove(deviceId, out _);
                                    device.KnownDevices.TryAdd(deviceId, new SparkplugBase<Metric>.KnownMetricStorage(_metrics.Values));

                                    await device.PublishDeviceBirthMessage(new List<Metric>(metrics), deviceId);
                                }
                            }

                            // Publish Device Data
                            await device.PublishDeviceData(new List<Metric>() { metric }, deviceId);

                            found = true;
                            break;
                        }
                    }
                }

                if (!found)
                {
                    // Publish Nodes
                    paths = _nodes.Keys.OrderByDescending(o => o);
                    foreach (var path in paths)
                    {
                        if (UnsPath.IsChildOf(path, message.Path))
                        {
                            var name = UnsPath.GetRelativeTo(path, message.Path);
                            var dataType = GetDataType(message.ContentType);
                            var value = System.Text.Encoding.UTF8.GetString(message.Content);

                            var metric = new Metric(name, dataType, value, message.Timestamp);

                            var node = _nodes.GetValueOrDefault(path);
                            if (node != null)
                            {
                                _metrics.Remove(message.Path);
                                _metrics.Add(message.Path, metric);

                                node.KnownDevices.TryRemove(message.Path, out _);
                                node.KnownDevices.TryAdd(message.Path, new SparkplugBase<Metric>.KnownMetricStorage(_metrics.Values));

                                await node.PublishMetrics(new List<Metric>() { metric });

                                break;
                            }
                        }
                    }
                }
            }
        }


        private static DataType GetDataType(UnsEventContentType contentType)
        {
            switch (contentType)
            {
                case UnsEventContentType.STRING: return DataType.String;
                case UnsEventContentType.BYTE: return DataType.Int8;
                case UnsEventContentType.INT_16: return DataType.Int16;
                case UnsEventContentType.INT_32: return DataType.Int32;
                case UnsEventContentType.INT_64: return DataType.Int64;
                case UnsEventContentType.FLOAT: return DataType.Float;
                case UnsEventContentType.DOUBLE: return DataType.Double;
                case UnsEventContentType.DECIMAL: return DataType.Double;
            }

            return DataType.String;
        }
    }
}
