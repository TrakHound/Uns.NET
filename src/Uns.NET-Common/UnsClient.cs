// Copyright (c) 2024 TrakHound Inc., All Rights Reserved.
// TrakHound Inc. licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uns
{
    public class UnsClient
    {
        private readonly Dictionary<string, NamespaceConfiguration> _namespaces = new Dictionary<string, NamespaceConfiguration>();
        private readonly Dictionary<string, string> _namespaceCache = new Dictionary<string, string>();
        private readonly ListDictionary<string, string> _patternMatches = new ListDictionary<string, string>();
        private readonly ListDictionary<string, string> _patternNoMatches = new ListDictionary<string, string>();
        private readonly Dictionary<string, IUnsInputConnection> _inputConnections = new Dictionary<string, IUnsInputConnection>();
        private readonly Dictionary<string, IUnsOutputConnection> _outputConnections = new Dictionary<string, IUnsOutputConnection>();
        private readonly Dictionary<string, IUnsConsumer> _consumers = new Dictionary<string, IUnsConsumer>();
        private readonly Dictionary<string, IUnsClientMiddleware> _middlewares = new Dictionary<string, IUnsClientMiddleware>();
        private readonly object _lock = new object();


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


        public event EventHandler<UnsEventMessage> EventReceived;


        public async Task Start()
        {
            var connections = new List<IUnsConnection>();

            foreach (var connection in _inputConnections.Values)
            {
                if (!connections.Contains(connection)) connections.Add(connection);
            }

            foreach (var connection in _outputConnections.Values)
            {
                if (!connections.Contains(connection)) connections.Add(connection);
            }

            foreach (var connection in connections)
            {
                await connection.Start();
            }
        }

        public async Task Stop()
        {
            var connections = new List<IUnsConnection>();

            foreach (var connection in _inputConnections.Values)
            {
                if (!connections.Contains(connection)) connections.Add(connection);
            }

            foreach (var connection in _outputConnections.Values)
            {
                if (!connections.Contains(connection)) connections.Add(connection);
            }

            foreach (var connection in connections)
            {
                await connection.Stop();
            }
        }


        public void AddNamespace(NamespaceConfiguration configuration)
        {
            if (configuration != null && !string.IsNullOrEmpty(configuration.Path))
            {
                _namespaces.Remove(configuration.Path);
                _namespaces.Add(configuration.Path, configuration);
            }
        }

        public NamespaceConfiguration GetNamespace(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                // Check cache for namespace ID (Path)
                _namespaceCache.TryGetValue(path, out var cachedId);
                if (cachedId == null)
                {
                    foreach (var configuration in _namespaces.Values.OrderByDescending(o => o.Path))
                    {
                        // Check if 'path' is a Child of the Namespace's Path
                        if (UnsPath.IsChildOf(configuration.Path, path))
                        {
                            // Add to Cache
                            _namespaceCache.Add(path, configuration.Path);
                            return configuration;
                        }
                    }
                }
                else
                {
                    _namespaces.TryGetValue(cachedId, out var configuration);
                    return configuration;
                }
            }

            return null;
        }


        public void AddInputConnection(IUnsInputConnection connection)
        {
            if (connection != null && !string.IsNullOrEmpty(connection.Id))
            {
                connection.EventReceived += ConnectionEventReceived;
                _inputConnections.Remove(connection.Id);
                _inputConnections.Add(connection.Id, connection);
            }
        }

        public void AddOutputConnection(IUnsOutputConnection connection)
        {
            if (connection != null && !string.IsNullOrEmpty(connection.Id))
            {
                _outputConnections.Remove(connection.Id);
                _outputConnections.Add(connection.Id, connection);
            }
        }

        public void AddConnection(IUnsConnection connection, UnsConnectionType type = UnsConnectionType.Both)
        {
            if (connection != null && !string.IsNullOrEmpty(connection.Id))
            {
                switch (type)
                {
                    case UnsConnectionType.Input:

                        if (typeof(IUnsInputConnection).IsAssignableFrom(connection.GetType()))
                        {
                            var inputConnection = (IUnsInputConnection)connection;
                            inputConnection.EventReceived += ConnectionEventReceived;
                            _inputConnections.Remove(inputConnection.Id);
                            _inputConnections.Add(inputConnection.Id, inputConnection);
                        }
                        break;

                    case UnsConnectionType.Output:

                        if (typeof(IUnsOutputConnection).IsAssignableFrom(connection.GetType()))
                        {
                            var outputConnection = (IUnsOutputConnection)connection;
                            _outputConnections.Remove(outputConnection.Id);
                            _outputConnections.Add(outputConnection.Id, outputConnection);
                        }
                        break;

                    case UnsConnectionType.Both:

                        if (typeof(IUnsInputConnection).IsAssignableFrom(connection.GetType()) &&
                            typeof(IUnsOutputConnection).IsAssignableFrom(connection.GetType()))
                        {
                            ((IUnsInputConnection)connection).EventReceived += ConnectionEventReceived;
                            _inputConnections.Remove(connection.Id);
                            _inputConnections.Add(connection.Id, (IUnsInputConnection)connection);
                            _outputConnections.Remove(connection.Id);
                            _outputConnections.Add(connection.Id, (IUnsOutputConnection)connection);
                        }
                        break;
                }
            }
        }

        public IUnsInputConnection GetInputConnection(string connectionId)
        {
            if (!string.IsNullOrEmpty(connectionId))
            {
                _inputConnections.TryGetValue(connectionId, out var connection);
                return connection;
            }

            return null;
        }

        public IUnsOutputConnection GetOutputConnection(string connectionId)
        {
            if (!string.IsNullOrEmpty(connectionId))
            {
                _outputConnections.TryGetValue(connectionId, out var connection);
                return connection;
            }

            return null;
        }


        public void AddMiddleware(IUnsClientMiddleware middleware)
        {
            if (middleware != null && !string.IsNullOrEmpty(middleware.Id))
            {
                _middlewares.Remove(middleware.Id);
                _middlewares.Add(middleware.Id, middleware);
            }
        }


        public UnsConsumer Subscribe(string pattern)
        {
            var consumer = new UnsConsumer(pattern);

            lock (_lock)
            {
                if (!_consumers.ContainsKey(consumer.Id))
                {
                    _consumers.Add(consumer.Id, consumer);
                }
            }

            return consumer;
        }

        public UnsConsumer<TResult> Subscribe<TResult>(string pattern)
        {
            var consumer = new UnsConsumer<TResult>(pattern);

            lock (_lock)
            {
                if (!_consumers.ContainsKey(consumer.Id))
                {
                    _consumers.Add(consumer.Id, consumer);
                }
            }

            return consumer;
        }

        public UnsJsonConsumer<TModel> SubscribeJson<TModel>(string pattern)
        {
            var consumer = new UnsJsonConsumer<TModel>(pattern);

            lock (_lock)
            {
                if (!_consumers.ContainsKey(consumer.Id))
                {
                    _consumers.Add(consumer.Id, consumer);
                }
            }

            return consumer;
        }


        private void ConnectionEventReceived(IUnsConnection connection, UnsEventMessage message)
        {
            ProcessEvent(connection, message);
        }

        private void ProcessEvent(IUnsConnection connection, UnsEventMessage message)
        {
            if (message != null && !string.IsNullOrEmpty(message.Path) && message.Content != null)
            {
                IEnumerable<IUnsConsumer> consumers;
                lock (_lock) consumers = _consumers.Values;
                foreach (var consumer in consumers)
                {
                    if (MatchPattern(consumer.Pattern, message.Path))
                    {
                        var sendEvent = ProcessMiddleware(message);
                        if (sendEvent != null)
                        {
                            sendEvent.Connection = connection;
                            sendEvent.Namespace = GetNamespace(message.Path);

                            consumer.Push(sendEvent);
                        }
                    }
                }
            }
        }

        private UnsEventMessage ProcessMiddleware(UnsEventMessage message)
        {
            if (message != null && _middlewares.Count > 0)
            {
                var outputMessage = message;
                foreach (var middleware in _middlewares.Values)
                {
                    outputMessage = middleware.Process(outputMessage);
                    if (outputMessage == null) return null;
                }
                return outputMessage;
            }

            return message;
        }

        private bool MatchPattern(string pattern, string path)
        {
            if (!string.IsNullOrEmpty(pattern) && !string.IsNullOrEmpty(path))
            {
                if (!_patternMatches.Contains(pattern, path))
                {
                    if (!_patternNoMatches.Contains(path, pattern))
                    {
                        bool matched;

                        if (pattern == "#")
                        {
                            matched = true;
                        }
                        else if (pattern.EndsWith("#"))
                        {
                            matched = UnsPath.IsChildOf(pattern.Remove(pattern.Length - 2), path);
                        }
                        else if (pattern.EndsWith("+"))
                        {
                            matched = UnsPath.GetParentPath(path) == pattern.Remove(pattern.Length - 2);
                        }
                        else if (!pattern.StartsWith("/"))
                        {
                            matched = UnsPath.GetObject(path) == pattern;
                        }
                        else
                        {
                            matched = pattern == path;
                        }

                        if (matched)
                        {
                            _patternMatches.Add(pattern, path);
                        }
                        else
                        {
                            _patternNoMatches.Add(path, pattern);
                        }

                        return matched;
                    }
                }
                else
                {
                    return true;
                }
            }

            return false;
        }


        public async Task Publish(string path, object content)
        {
            if (content != null)
            {
                await Publish(path, UnsEventMessage.GetContentType(content.GetType()), Encoding.UTF8.GetBytes(content.ToString()));
            }
        }

        public async Task Publish(string connectionId, string path, object content)
        {
            if (content != null)
            {
                await Publish(connectionId, path, UnsEventMessage.GetContentType(content.GetType()), Encoding.UTF8.GetBytes(content.ToString()));
            }
        }

        public async Task PublishJson(string path, object content)
        {
            if (content != null)
            {
                var json = Json.Convert(content);
                if (!string.IsNullOrEmpty(json))
                {
                    await Publish(path, UnsEventContentType.JSON, Encoding.UTF8.GetBytes(json));
                }
            }
        }

        public async Task PublishJson(string connectionId, string path, object content)
        {
            if (content != null)
            {
                var json = Json.Convert(content);
                if (!string.IsNullOrEmpty(json))
                {
                    await Publish(connectionId, path, UnsEventContentType.JSON, Encoding.UTF8.GetBytes(json));
                }
            }
        }

        public async Task Publish(string path, UnsEventContentType contentType, byte[] content)
        {
            if (!string.IsNullOrEmpty(path) && content != null && content.Length > 0)
            {
                var message = new UnsEventMessage();
                message.Path = path;
                message.Namespace = GetNamespace(path);
                message.ContentType = contentType;
                message.Content = content;

                await Publish(message);
            }
        }

        public async Task Publish(string connectionId, string path, UnsEventContentType contentType, byte[] content)
        {
            if (!string.IsNullOrEmpty(connectionId) && !string.IsNullOrEmpty(path) && content != null && content.Length > 0)
            {
                var message = new UnsEventMessage();
                message.Path = path;
                message.Namespace = GetNamespace(path);
                message.ContentType = contentType;
                message.Content = content;

                await Publish(connectionId, message);
            }
        }

        public async Task Publish(UnsEventMessage message)
        {
            if (!string.IsNullOrEmpty(message.Path) && message.Content != null && message.Content.Length > 0)
            {
                foreach (var connection in _outputConnections.Values)
                {
                    var sendMessage = ProcessMiddleware(message);
                    if (sendMessage != null)
                    {
                        sendMessage.Connection = connection;
                        sendMessage.Namespace = GetNamespace(sendMessage.Path);

                        await connection.Publish(sendMessage);
                    }
                }
            }
        }

        public async Task Publish(string connectionId, UnsEventMessage message)
        {
            if (!string.IsNullOrEmpty(connectionId) && !string.IsNullOrEmpty(message.Path) && message.Content != null && message.Content.Length > 0)
            {
                _outputConnections.TryGetValue(connectionId, out var connection);
                if (connection != null)
                {
                    var sendMessage = ProcessMiddleware(message);
                    if (sendMessage != null)
                    {
                        sendMessage.Connection = connection;
                        sendMessage.Namespace = GetNamespace(sendMessage.Path);

                        await connection.Publish(sendMessage);
                    }
                }
            }
        }
    }
}
