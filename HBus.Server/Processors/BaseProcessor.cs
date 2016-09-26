using System;
using System.Collections.Generic;
using System.Reflection;
using HBus.Server.Data;
using log4net;

namespace HBus.Server.Processors
{
    /// <summary>
    /// Endpoint base class
    /// </summary>
    public abstract class BaseProcessor //: IObserver<Event>, IObservable<Event>, IDisposable
    {
        protected static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IDictionary<BaseProcessor, EventFilter[]> _subscriptions;

        protected BaseProcessor()
        {
            _subscriptions = new Dictionary<BaseProcessor, EventFilter[]>();
        }

        public virtual void Start()
        {
            
        }

        public virtual void Stop()
        {

        }
        /// <summary>
        /// Event handler
        /// </summary>
        public Action<Event, BaseProcessor> OnSourceEvent { get; set; }

        /// <summary>
        /// Error handler
        /// </summary>
        public Action<Exception, BaseProcessor> OnSourceError { get; set; }

        public Action<BaseProcessor> OnSourceClose { get; set; }

        /// <summary>
        /// Send event to all subscribers
        /// </summary>
        /// <param name="event"></param>
        /// <param name="sender"></param>
        public void Send(Event @event, BaseProcessor sender)
        {
            var i = 0;
            foreach (var s in _subscriptions)
            {
                var ok = false;

                //Filter event
                if (s.Value != null)
                    foreach (var filter in s.Value)
                    {
                        if (filter.Evaluate(@event)) ok = true;
                    }
                else
                {
                    ok = true;
                }

                //Send event to subscriber
                if (ok && s.Key.OnSourceEvent != null)
                    s.Key.OnSourceEvent(@event, sender);
                i++;
            }
            Log.Debug(string.Format("Event {0} sent to {1} subscribers", @event, i));
        }

        /// <summary>
        /// Send error to subscribers
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="sender"></param>
        public void Error(Exception exception, BaseProcessor sender)
        {
            foreach (var s in _subscriptions)
            {
                //Send error to subscribers
                if (s.Key.OnSourceError != null)
                    s.Key.OnSourceError(exception, sender);
            }
            Log.Error(string.Format("error {0} propagated to subscribers", exception));
        }

        /// <summary>
        /// Close endpoint
        /// </summary>
        public void Close()
        {
            foreach (var s in _subscriptions)
            {
                //Send close event to subscribers
                if (s.Key.OnSourceClose != null)
                    s.Key.OnSourceClose(this);
            }

            Log.Error(string.Format("Endpoint {0} closed", this));
        }
        /// <summary>
        /// Subscribe client endpoint to specific or all events
        /// </summary>
        /// <param name="subscriber"></param>
        /// <param name="filters"></param>
        public void AddSubscriber(BaseProcessor subscriber, EventFilter[] filters = null)
        {
            if (!_subscriptions.ContainsKey(subscriber))
                _subscriptions.Add(subscriber, filters);
            else
                _subscriptions[subscriber] = filters;

            Log.Debug(string.Format("Subscriber {0} added", subscriber));
        }

        /// <summary>
        /// Unsubscribe client from current endpoint
        /// </summary>
        /// <param name="subscriber"></param>
        public void DeleteSubscriber(BaseProcessor subscriber)
        {
            if (_subscriptions.ContainsKey(subscriber))
                _subscriptions.Remove(subscriber);

            Log.Debug(string.Format("Subscriber {0} removed", subscriber));
        }

        /*
        #region Observer events
        /// <summary>
        /// Process event and send to subscribers
        /// </summary>
        /// <param name="value">event</param>
        public void OnNext(Event value)
        {
            Send(value, this);

            Log.Debug(string.Format("Event {0} processed", value));
        }

        public void OnError(Exception error)
        {
            //TODO: intercept connection errors and close clients
            Error(error, this);

            Log.Error("Error from subscriber", error);
        }

        /// <summary>
        /// End of observer subscription
        /// </summary>
        public void OnCompleted()
        {

            Log.Debug("Event source completed");
        }
        #endregion

        #region  IObservable
        public IDisposable Subscribe(IObserver<Event> observer)
        {
            if (!_observers.Contains(observer))
                _observers.Add(observer);
            return this;
        }

        public void Dispose()
        {
            foreach (var observer in _observers)
            {
                observer.OnCompleted();
            }
            _observers.Clear();
        }
        #endregion
        */
    }   
}