// Copyright (c) 2024 TrakHound Inc., All Rights Reserved.
// TrakHound Inc. licenses this file to you under the MIT license.

using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Uns
{
    public class UnsMqttConnection : UnsConnectionBase
    {
        private const string _type = "MQTT";

        private readonly string _id;
        private readonly List<string> _patterns = new List<string>();
        private readonly UnsEventContentType _contentType;
        private readonly MqttFactory _mqttFactory;
        private readonly IMqttClient _mqttClient;
        private readonly UnsMqttConnectionConfiguration _configuration;
        private readonly List<Subscription> _subscriptions = new List<Subscription>();
        private readonly List<Destination> _destinations = new List<Destination>();

        private CancellationTokenSource _stop;
        private UnsConnectionStatus _connectionStatus;
        private long _lastResponse;


        struct Subscription
        {
            public string Topic { get; set; }
            public string BasePath { get; set; }
            public MqttQualityOfServiceLevel QoS { get; set; }
        }

        struct Destination
        {
            public string BasePath { get; set; }
            public string Topic { get; set; }
        }


        public override string Type => _type;

        public override string Id => _id;

        public override IEnumerable<string> Patterns => _patterns;

        /// <summary>
        /// Gets the Client Configuration
        /// </summary>
        public UnsMqttConnectionConfiguration Configuration => _configuration;

        /// <summary>
        /// Gets the Unix Timestamp (in Milliseconds) since the last response from the MTConnect Agent
        /// </summary>
        public long LastResponse => _lastResponse;

        /// <summary>
        /// Gets the status of the connection to the MQTT broker
        /// </summary>
        public UnsConnectionStatus ConnectionStatus => _connectionStatus;


        /// <summary>
        /// Raised when an error occurs during connection to the MQTT broker
        /// </summary>
        public event EventHandler<Exception> ConnectionError;

        /// <summary>
        /// Raised when an Internal Error occurs
        /// </summary>
        public event EventHandler<Exception> InternalError;

        /// <summary>
        /// Raised when a Message is received
        /// </summary>
        public event EventHandler<MqttApplicationMessage> MessageReceived;

        //public Func<MqttApplicationMessage, Task> MessageReceived;

        /// <summary>
        /// Raised when any Response from the Client is received
        /// </summary>
        public event EventHandler ResponseReceived;

        /// <summary>
        /// Raised when the Client is Starting
        /// </summary>
        public event EventHandler ClientStarting;

        /// <summary>
        /// Raised when the Client is Started
        /// </summary>
        public event EventHandler ClientStarted;

        /// <summary>
        /// Raised when the Client is Stopping
        /// </summary>
        public event EventHandler ClientStopping;

        /// <summary>
        /// Raised when the Client is Stopeed
        /// </summary>
        public event EventHandler ClientStopped;


        public UnsMqttConnection(string server, int port = 1883, string id = null)
        {
            _id = !string.IsNullOrEmpty(id) ? id : Guid.NewGuid().ToString();

            var configuration = new UnsMqttConnectionConfiguration();
            configuration.Server = server;
            configuration.Port = port;
            _configuration = configuration;

            _mqttFactory = new MqttFactory();
            _mqttClient = _mqttFactory.CreateMqttClient();
            _mqttClient.ApplicationMessageReceivedAsync += ProcessMessage;
        }

        public UnsMqttConnection(UnsMqttConnectionConfiguration configuration)
        {
            _configuration = configuration;
            if (_configuration == null) _configuration = new UnsMqttConnectionConfiguration();

            _mqttFactory = new MqttFactory();
            _mqttClient = _mqttFactory.CreateMqttClient();
            _mqttClient.ApplicationMessageReceivedAsync += ProcessMessage;
        }



        public override Task Start()
        {
            _stop = new CancellationTokenSource();

            ClientStarting?.Invoke(this, new EventArgs());

            _ = Task.Run(Worker, _stop.Token);

            return Task.CompletedTask;
        }

        public override Task Stop()
        {
            ClientStopping?.Invoke(this, new EventArgs());

            if (_stop != null) _stop.Cancel();

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (_mqttClient != null) _mqttClient.Dispose();
        }

        public override async Task Publish(UnsEventMessage message)
        {
            foreach (var destination in _destinations)
            {
                if (UnsPath.IsChildOf(destination.BasePath, message.Path))
                {
                    var publishMessage = new MqttApplicationMessage();
                    publishMessage.Topic = UnsPath.Combine(destination.Topic, message.Path);
                    //message.Retain = retain;
                    publishMessage.Payload = message.Content;

                    await Publish(publishMessage);
                }
            }
        }


        private async Task Worker()
        {
            do
            {
                try
                {
                    try
                    {
                        // Declare new MQTT Client Options with Tcp Server
                        var clientOptionsBuilder = new MqttClientOptionsBuilder().WithTcpServer(_configuration.Server, _configuration.Port);

                        clientOptionsBuilder.WithCleanSession(false);

                        // Set Client ID
                        if (!string.IsNullOrEmpty(_configuration.ClientId))
                        {
                            clientOptionsBuilder.WithClientId(_configuration.ClientId);
                        }

                        var certificates = new List<X509Certificate2>();

                        // Add CA (Certificate Authority)
                        if (!string.IsNullOrEmpty(_configuration.CertificateAuthority))
                        {
                            certificates.Add(new X509Certificate2(GetFilePath(_configuration.CertificateAuthority)));
                        }

                        // Add Client Certificate & Private Key
                        if (!string.IsNullOrEmpty(_configuration.PemCertificate) && !string.IsNullOrEmpty(_configuration.PemPrivateKey))
                        {

#if NET5_0_OR_GREATER
                            certificates.Add(new X509Certificate2(X509Certificate2.CreateFromPemFile(GetFilePath(_configuration.PemCertificate), GetFilePath(_configuration.PemPrivateKey)).Export(X509ContentType.Pfx)));
#else
                    throw new Exception("PEM Certificates Not Supported in .NET Framework 4.8 or older");
#endif

                            clientOptionsBuilder.WithTls(new MqttClientOptionsBuilderTlsParameters()
                            {
                                UseTls = true,
                                SslProtocol = System.Security.Authentication.SslProtocols.Tls12,
                                IgnoreCertificateRevocationErrors = _configuration.AllowUntrustedCertificates,
                                IgnoreCertificateChainErrors = _configuration.AllowUntrustedCertificates,
                                AllowUntrustedCertificates = _configuration.AllowUntrustedCertificates,
                                Certificates = certificates
                            });
                        }

                        // Add Credentials
                        if (!string.IsNullOrEmpty(_configuration.Username) && !string.IsNullOrEmpty(_configuration.Password))
                        {
                            if (_configuration.UseTls)
                            {
                                clientOptionsBuilder.WithCredentials(_configuration.Username, _configuration.Password).WithTls();
                            }
                            else
                            {
                                clientOptionsBuilder.WithCredentials(_configuration.Username, _configuration.Password);
                            }
                        }

                        // Build MQTT Client Options
                        var clientOptions = clientOptionsBuilder.Build();

                        // Connect to the MQTT Client
                        await _mqttClient.ConnectAsync(clientOptions);

                        UpdateConnectionStatus(UnsConnectionStatus.Connected);

                        // Subscribe
                        await Subscribe();

                        ClientStarted?.Invoke(this, new EventArgs());

                        while (_mqttClient.IsConnected && !_stop.IsCancellationRequested)
                        {
                            await Task.Delay(100);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ConnectionError != null) ConnectionError.Invoke(this, ex);
                    }

                    UpdateConnectionStatus(UnsConnectionStatus.Disconnected);

                    await Task.Delay(_configuration.RetryInterval, _stop.Token);
                }
                catch (TaskCanceledException) { }
                catch (Exception ex)
                {
                    InternalError?.Invoke(this, ex);
                }

            } while (!_stop.Token.IsCancellationRequested);


            try
            {
                // Disconnect from the MQTT Client
                if (_mqttClient != null) await _mqttClient.DisconnectAsync(MqttClientDisconnectOptionsReason.NormalDisconnection);
            }
            catch { }


            ClientStopped?.Invoke(this, new EventArgs());
        }


        public void AddSubscription(string topic, string basePath = null, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtMostOnce)
        {
            if (!string.IsNullOrEmpty(topic))
            {
                var subscription = new Subscription();
                subscription.Topic = topic;
                subscription.BasePath = basePath;
                subscription.QoS = qos;
                _subscriptions.Add(subscription);
                _patterns.Add(topic);
            }
        }

        public void AddSubscription(IEnumerable<string> topics, string basePath = null, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtMostOnce)
        {
            if (!topics.IsNullOrEmpty())
            {
                foreach (var topic in topics)
                {
                    AddSubscription(topic, basePath, qos);
                }
            }
        }

        public void AddDestination(string pattern, string topic = null)
        {
            if (!string.IsNullOrEmpty(pattern))
            {
                var destination = new Destination();
                destination.Topic = topic;
                destination.BasePath = pattern;
                _destinations.Add(destination);
                _patterns.Add(topic);
            }
        }


        private async Task Subscribe()
        {
            if (_mqttClient.IsConnected)
            {
                var topicFilters = new List<MqttTopicFilter>();
                foreach (var subscription in _subscriptions)
                {
                    var topicFilter = new MqttTopicFilter();
                    topicFilter.Topic = subscription.Topic;
                    topicFilter.QualityOfServiceLevel = subscription.QoS;
                    topicFilters.Add(topicFilter);
                }

                var subscribeOptions = new MqttClientSubscribeOptions();
                subscribeOptions.TopicFilters = topicFilters;

                await _mqttClient.SubscribeAsync(subscribeOptions, _stop.Token);
            }
        }

        private Task ProcessMessage(MqttApplicationMessageReceivedEventArgs args)
        {
            foreach (var subscription in _subscriptions)
            {
                if (UnsPath.MatchPattern(subscription.Topic, args.ApplicationMessage.Topic))
                {
                    var pattern = subscription.Topic.TrimEnd('#').TrimEnd('+');

                    var message = new UnsEventMessage();
                    message.Path = UnsPath.Combine(subscription.BasePath, UnsPath.GetRelativeTo(pattern, args.ApplicationMessage.Topic));
                    message.ContentType = _contentType;
                    message.Content = args.ApplicationMessage.Payload;

                    ReceiveEvent(message);
                }
            }

            return Task.CompletedTask;
        }

        private async Task Publish(MqttApplicationMessage message)
        {
            if (_mqttClient.IsConnected && message != null && !string.IsNullOrEmpty(message.Topic))
            {
                await _mqttClient.PublishAsync(message);
            }
        }


        private static string GetFilePath(string path)
        {
            var x = path;
            if (!Path.IsPathRooted(x))
            {
                x = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, x);
            }

            return x;
        }
    }
}
