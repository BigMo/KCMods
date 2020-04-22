using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Newtonsoft.Json;

/// <summary>
/// Provides functionality for communication between mods.
/// </summary>
namespace Zat.Shared.InterModComm
{
    /// <summary>
    /// MonoBehaviour that receives messages and allows to send messages to other instances yourself.
    /// </summary>
    public class IMCPort : MonoBehaviour
    {
        private Component component;
        private List<IMCWait> pendingResponses = new List<IMCWait>();
        private Dictionary<string, UnityAction<IRequestHandler, IMCMessage>> receiveListeners = new Dictionary<string, UnityAction<IRequestHandler, IMCMessage>>();
        private Dictionary<string, Component> ports = new Dictionary<string, Component>();

        private class RequestEvent : UnityEvent<IRequestHandler> { }
        public static KCModHelper helper;
        public void Update()
        {
            //Process timed out requests
            var timedOut = pendingResponses.Where(p => p.TimedOut);
            pendingResponses = pendingResponses.Except(timedOut).ToList();

            foreach (var timeOut in timedOut)
                timeOut.OnTimeout?.Invoke();
        }

        /// <summary>
        /// Called by Component.SendMessage
        /// </summary>
        /// <param name="data">Object array that can be unpacked to an IMCMessage</param>
        private void ReceiveMessage(object[] data)
        {
            try
            {
                var message = IMCMessage.FromUnityMessage(data);
                if (message.Type == IMCMessage.MessageType.Response)
                {
                    var pending = pendingResponses.FirstOrDefault(p => p.ID == message.ID);
                    if (pending == null)
                    {
                        //TODO: Error handling: Received a response to a request that we didn't send!
                        return;
                    }

                    pending.OnReceive(message);
                    pendingResponses.Remove(pending);
                }
                else
                {
                    var handler = new IMCRequestHandler(message);
                    if (!receiveListeners.ContainsKey(message.Name))
                        throw new Exception($"Unhandled request: {message.ToString()}");
                    else
                    {
                        receiveListeners[message.Name]?.Invoke(handler, message);
                        if (handler.Response != null)
                        {
                            SendIMCMessage(message.Source, handler.Response, 0f, null, null);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (helper != null)
                {
                    helper.Log($"Failed to process message: {ex.Message}");
                    helper.Log($"Failed to process message: {ex.StackTrace}");
                }
            }
        }

        #region Registration of Receive Listener
        /// <summary>
        /// Registers a listener that is called when the specified function was requested, not passing any payload
        /// </summary>
        /// <param name="name">The name of the function</param>
        /// <param name="callback">The callback to be fired when this function was requested</param>
        public void RegisterReceiveListener(string name, UnityAction<IRequestHandler, string> callback)
        {
            var cb = new UnityAction<IRequestHandler, IMCMessage>(
                (handler, msg) =>
                {
                    callback(handler, msg.Source);
                }
            );
            if (receiveListeners.ContainsKey(name)) throw new Exception($"Function \"{name}\" was already registered!");
            receiveListeners[name] = cb;
        }

        /// <summary>
        /// Registers a listener that is called when the specified function was requested, passing along the specified payload
        /// </summary>
        /// <typeparam name="T">The type of the payload that was passed to the function</typeparam>
        /// <param name="name">The name of the function</param>
        /// <param name="callback">The callback to be fired when this function was requested</param>
        public void RegisterReceiveListener<T>(string name, UnityAction<IRequestHandler, string, T> callback)
        {
            var cb = new UnityAction<IRequestHandler, IMCMessage>(
                (handler, msg) =>
                {
                    callback(handler, msg.Source, msg.ParsePayload<T>());
                }
            );
            if (receiveListeners.ContainsKey(name)) throw new Exception($"Function \"{name}\" was already registered!");
            receiveListeners[name] = cb;
        }
        #endregion

        private Component GetIMCPort(string objectName)
        {
            if (ports.ContainsKey(objectName)) return ports[objectName];
            var port = GetIMCPort(GameObject.Find(objectName));
            if (port) ports[objectName] = port;
            return port;
        }
        private Component GetIMCPort(GameObject go)
        {
            return go?.GetComponents<Component>()?.FirstOrDefault(c => c.GetType().Name == nameof(IMCPort)) as Component;
        }

        private void SendIMCMessage(string _target, IMCMessage message, float timeout, UnityAction<IMCMessage> onReceive, UnityAction onTimeout)
        {
            var target = GetIMCPort(_target);
            if (target == null) throw new SendMessageException("Target not set");
            if (!target.gameObject.activeInHierarchy) throw new SendMessageException($"Receiving GameObject \"{target.name}\" is inactive");

            if (onReceive != null && onTimeout != null)
            {
                pendingResponses.Add(new IMCWait(
                    message.ID,
                    Time.time + timeout,
                    onReceive,
                    onTimeout)
                );
            }
            target.SendMessage(nameof(ReceiveMessage), message.ToObjects());
        }

        #region Remote Prodcedure Calls
        /// <summary>
        /// Calls a remote function using the given payload, expecting no return value
        /// </summary>
        /// <typeparam name="P">The type of the payload to pass to the function</typeparam>
        /// <param name="target">The target port to send the message to</param>
		/// <param name="name">The name of the remote function</param>
        /// <param name="payload">The payload to pass to the function</param>
        /// <param name="timeout">The amount of time, in seconds, to wait for a response</param>
        /// <param name="callback">The callback to call upon receiving a response</param>
        /// <param name="error">The callback to call upon encountering errors
        public void RPC<P>(string target, string name, P payload, float timeout, UnityAction callback, UnityAction<Exception> error)
        {
            try
            {
                var message = IMCMessage.CreateRequest(this.name, name, JsonConvert.SerializeObject(payload));

                SendIMCMessage(
                    target,
                    message,
                    timeout,
                    (msg) => { callback?.Invoke(); },
                    () => { error.Invoke(TimeoutException.Instance); });
            }
            catch (Exception ex)
            {
                error?.Invoke(ex);
            }
        }
        /// <summary>
        /// Calls a remote function using the given payload, expecting a return value of a specific type
        /// </summary>
        /// <typeparam name="P">The type of the payload to pass to the function</typeparam>
        /// <typeparam name="R">The type of the return value of the called function</typeparam>
        /// <param name="target">The target port to send the message to</param>
        /// <param name="name">The name of the remote function</param>
        /// <param name="payload">The payload to pass to the function</param>
        /// <param name="timeout">The amount of time, in seconds, to wait for a response</param>
        /// <param name="callback">The callback to call upon receiving a response</param>
        /// <param name="error">The callback to call upon encountering errors</param>
        public void RPC<P, R>(string target, string name, P payload, float timeout, UnityAction<R> callback, UnityAction<Exception> error)
        {
            try
            {
                var message = IMCMessage.CreateRequest(this.name, name, JsonConvert.SerializeObject(payload));

                SendIMCMessage(
                    target,
                    message,
                    timeout,
                    (msg) => { callback?.Invoke(msg.ParsePayload<R>()); },
                    () => { error.Invoke(TimeoutException.Instance); });
            }
            catch (Exception ex)
            {
                error?.Invoke(ex);
            }
        }
        /// <summary>
        /// Calls a remote function without payload, expecting no return value
        /// </summary>
        /// <param name="target">The target port to send the message to</param>
        /// <param name="name">The name of the remote function</param>
        /// <param name="timeout">The amount of time, in seconds, to wait for a response</param>
        /// <param name="callback">The callback to call upon receiving a response</param>
        /// <param name="error">The callback to call upon encountering errors
        public void RPC(string target, string name, float timeout, UnityAction callback, UnityAction<Exception> error)
        {
            try
            {
                var message = IMCMessage.CreateRequest(this.name, name);

                SendIMCMessage(
                    target,
                    message,
                    timeout,
                    (msg) => { callback?.Invoke(); },
                    () => { error.Invoke(TimeoutException.Instance); });
            }
            catch (Exception ex)
            {
                error?.Invoke(ex);
            }
        }
        /// <summary>
        /// Calls a remote function without payload, expecting a return value of a specific type
        /// </summary>
        /// <typeparam name="R">The type of the return value of the called function</typeparam>
        /// <param name="target">The target port to send the message to</param>
        /// <param name="name">The name of the remote function</param>
        /// <param name="payload">The payload to pass to the function</param>
        /// <param name="timeout">The amount of time, in seconds, to wait for a response</param>
        /// <param name="callback">The callback to call upon receiving a response</param>
        /// <param name="error">The callback to call upon encountering errors</param>
        public void RPC<R>(string target, string name, float timeout, UnityAction<R> callback, UnityAction<Exception> error)
        {
            try
            {
                var message = IMCMessage.CreateRequest(this.name, name);

                SendIMCMessage(
                    target,
                    message,
                    timeout,
                    (msg) => { callback?.Invoke(msg.ParsePayload<R>()); },
                    () => { error.Invoke(TimeoutException.Instance); });
            }
            catch (Exception ex)
            {
                error?.Invoke(ex);
            }
        }
        #endregion
    }
    /// <summary>
    /// Used when processing received messages
    /// </summary>
    public interface IRequestHandler
    {
        /// <summary>
        /// Sends a response to a request
        /// </summary>
        /// <param name="source">Where the response originates from</param>
        void SendResponse(string source);
        /// <summary>
        /// Sends a response to a request, passing along payload
        /// </summary>
        /// <typeparam name="T">The type of the payload to pass</typeparam>
        /// <param name="source">>Where the response originates from</param>
        /// <param name="payload">The data to be passed along with the response</param>
        void SendResponse<T>(string source, T payload);
        /// <summary>
        /// Sends a response to a request, passing along an error
        /// </summary>
        /// <param name="source">Where the response originates from</param>
        /// <param name="error">The error to pass along</param>
        void SendError(string source, string error);
        /// <summary>
        /// Sends a response to a request, passing along an error
        /// </summary>
        /// <param name="source">Where the response originates from</param>
        /// <param name="error">The error to pass along</param>
        void SendError(string source, Exception error);
    }
    class IMCRequestHandler : IRequestHandler
    {
        private IMCMessage message;
        public IMCMessage Response { get; private set; }

        public IMCRequestHandler(IMCMessage message)
        {
            this.message = message;
        }

        public void SendResponse(string source)
        {
            Response = message.RespondPayload(source);
        }

        public void SendResponse<T>(string source, T payload)
        {
            Response = message.RespondPayload(source, payload);
        }

        public void SendError(string source, string error)
        {
            Response = message.RespondError(source, error);
        }

        public void SendError(string source, Exception error)
        {
            this.SendError(source, error.Message);
        }
    }
    /// <summary>
    /// Thrown when a request message did not received a response
    /// </summary>
    public class TimeoutException : Exception
    {
        public static TimeoutException Instance = new TimeoutException();

        private TimeoutException() : base("The RPC timed out and was not answered in-time.") { }
    }
    /// <summary>
    /// Thrown when a port failed to send a message
    /// </summary>
    public class SendMessageException : Exception
    {
        public SendMessageException(string message) : base(message) { }
    }

    /// <summary>
    /// A simple class that holds information about a message that awaits a response.
    /// </summary>
    class IMCWait
    {
        /// <summary>
        /// ID of the message's response that is being awaited
        /// </summary>
        public Guid ID { get; private set; }
        /// <summary>
        /// Determines when the response must have been received
        /// </summary>
        public float ReceiveUntil { get; private set; }
        /// <summary>
        /// Called when the response was received
        /// </summary>
        public UnityAction<IMCMessage> OnReceive { get; private set; }
        /// <summary>
        /// Called when the response was not received in time
        /// </summary>
        public UnityAction OnTimeout { get; private set; }
        /// <summary>
        /// Returns whether or not the response timed out yet
        /// </summary>
        public bool TimedOut { get { return Time.time <= ReceiveUntil; } }

        public IMCWait(Guid id, float receiveUntil, UnityAction<IMCMessage> onReceive, UnityAction onTimeout)
        {
            ID = id;
            ReceiveUntil = receiveUntil;
            OnReceive = onReceive;
            OnTimeout = onTimeout;
        }
    }

    /// <summary>
    /// Object representing messages sent between mods.
    /// </summary>
    class IMCMessage
    {
        /// <summary>
        /// The name of the GameObject the message originated from.
        /// </summary>
        public string Source { get; private set; }
        /// <summary>
        /// The name of this message, usually the name of a remote function.
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// Unique ID of this message. Used to detect responses.
        /// </summary>
        public Guid ID { get; private set; }
        /// <summary>
        /// Whether this message is a request or a response.
        /// </summary>
        public MessageType Type { get; private set; }
        /// <summary>
        /// Payload of this message, usually a JSON string.
        /// </summary>
        public string Payload { get; private set; }
        /// <summary>
        /// Error message of a response, usually the Message of an Exception.
        /// </summary>
        public string Error { get; private set; }
        /// <summary>
        /// Whether or not this message contains an error.
        /// </summary>
        public bool IsError { get { return !string.IsNullOrEmpty(Error); } }
        /// <summary>
        /// Whether a message is a request or a response.
        /// </summary>
        public enum MessageType : int { Request = 0, Response }

        /// <summary>
        /// Unpacks an IMCMessage from an object array
        /// </summary>
        /// <param name="data">Source, Name, Type, ID, Payload and Error of a message</param>
        /// <returns></returns>
        public static IMCMessage FromUnityMessage(object[] data)
        {
            if (data == null || data.Length < 6) throw new Exception("Malformed SendMessage-data: data null or too short");
            return new IMCMessage((string)data[0], (string)data[1], (MessageType)data[3], new Guid(((byte[])data[2])), (string)data[4], (string)data[5]);
        }
        /// <summary>
        /// Creates a request.
        /// </summary>
        /// <param name="source">Where this message originates from</param>
        /// <param name="name">Name of this message</param>
        /// <param name="payload">Payload of this message (optional)</param>
        /// <returns></returns>
        public static IMCMessage CreateRequest(string source, string name, string payload = null)
        {
            return new IMCMessage(source, name, MessageType.Request, Guid.NewGuid(), payload);
        }
        private IMCMessage(string source, string name, MessageType type, Guid id, string payload = null, string error = null)
        {
            Source = source;
            Name = name;
            Type = type;
            ID = id == Guid.Empty ? Guid.NewGuid() : id;
            Payload = payload;
        }
        /// <summary>
        /// Creates a matching response message to this request message, passing along a payload
        /// </summary>
        /// <typeparam name="T">The type of the payload</typeparam>
        /// <param name="source">Where this message originates from</param>
        /// <param name="payload">Payload of this message, usually a return value</param>
        /// <returns></returns>
        public IMCMessage RespondPayload<T>(string source, T payload)
        {
            return new IMCMessage(source, Name, MessageType.Response, ID, payload != null ? JsonConvert.SerializeObject(payload) : null);
        }
        /// <summary>
        /// Creates a matching response message to this request message
        /// </summary>
        /// <param name="source">Where this message originates from</param>
        /// <returns></returns>
        public IMCMessage RespondPayload(string source)
        {
            return new IMCMessage(source, Name, MessageType.Response, ID, null);
        }
        /// <summary>
        /// Creates a matching error response message to this request message
        /// </summary>
        /// <param name="source">Where this message originates from</param>
        /// <param name="error">The error that was raised, usually the Message of an Exception</param>
        /// <returns></returns>
        public IMCMessage RespondError(string source, string error)
        {
            return new IMCMessage(source, Name, MessageType.Response, ID, null, error);
        }
        /// <summary>
        /// Attempts to deserialize this message's JSON payload into the specified type
        /// </summary>
        /// <typeparam name="T">The type to deserialize the payload into</typeparam>
        /// <returns></returns>
        public T ParsePayload<T>()
        {
            if (Payload == null) return default(T);
            try
            {
                return JsonConvert.DeserializeObject<T>(Payload);
            }
            catch
            {
                return default(T);
            }
        }
        /// <summary>
        /// Packs this message into an object array that can be used to send the message to other GameObjects.
        /// </summary>
        /// <returns></returns>
        public object[] ToObjects()
        {
            return new object[] { Source, Name, ID.ToByteArray(), (int)Type, Payload, Error };
        }

        public override string ToString()
        {
            return $"[{ID}:{Source}:{Name}] {Type}, Data: {Payload ?? "-"}, Error: {Error ?? "-"}";
        }
    }
}
