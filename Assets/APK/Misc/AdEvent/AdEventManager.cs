using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using PublicComponentCenter;
using UnityEngine;

namespace AdEvent
{
    [DisallowMultipleComponent, DefaultExecutionOrder(-1000)]
    public class AdEventManager : MonoBehaviour
    {
        private static readonly object SyncRoot = new object();
        private static volatile AdEventManager instance;

        private static AdEventManager Instance
        {
            get
            {
                if (instance != null) return instance;
                lock (SyncRoot)
                {
                    var o = new GameObject(nameof(AdEventManager));
                    DontDestroyOnLoad(o);
                    instance = o.AddComponent<AdEventManager>();
                }

                return instance;
            }
        }

        private IDictionary<string, AutoEvent> events;

        private void Awake() => transform.SetAsFirstSibling();

        private void OnEnable() => events = new Dictionary<string, AutoEvent>();

        private void OnDisable()
        {
            foreach (var id in events.Select(e=>e.Key).ToArray()) InternalReleaseEvent(id);
            events.Clear();
        }

        private AutoEvent InternalAcquireEvent(Action action = null) => InternalEvent(new AutoEvent(action));

        private AutoEvent InternalEvent(AutoEvent @event)
        {
            if (events.TryGetValue(@event.EventId, out var old)) InternalReleaseEvent(old);
            events.Add(@event.EventId, @event);
            if (GameEntry.Ad.NativeMsgReceiver.AddEvent(@event.EventId, id =>
            {
                if (@event.EventId.Equals(id)) @event.Action?.Invoke();
            })) $"event:{@event.EventId} register {"success".SetColor(Color.green)}".SetColor(Color.cyan).Log();
            else $"event:{@event.EventId} register {"failure".SetColor(Color.red)}".SetColor(Color.cyan).Log();
            return @event;
        }

        private void InternalReleaseEvent(string eventId)
        {
            if (!events.TryGetValue(eventId, out _)) return;
            events.Remove(eventId);
            if (GameEntry.Ad.NativeMsgReceiver.RemoveEvent(eventId))
                $"event:{eventId} unregister {"success".SetColor(Color.green)}".SetColor(Color.magenta).Log();
            else $"event:{eventId} unregister {"failure".SetColor(Color.red)}".SetColor(Color.magenta).Log();
        }

        private void InternalReleaseEvent(AutoEvent @event) => InternalReleaseEvent(@event.EventId);

        public static AutoEvent AcquireEvent(Action action=null) => Instance.InternalAcquireEvent(action);
        public static void ReleaseEvent(AutoEvent @event) => Instance.InternalReleaseEvent(@event);
        public static void ReleaseEvent(string eventId) => Instance.InternalReleaseEvent(eventId);

        public static void PostReward(Action action)
        {
            if (action == null) return;
            var @event = new AutoEvent(action) {EventId = "1145141919810"};
            Instance.InternalEvent(@event);
            @event.PostEvent();
        }
    }
    public class AutoEvent
    {
        [NotNull]protected internal string EventId { get; set; }
        public Action Action { get; set; }

        private AutoEvent() => EventId = Guid.NewGuid().ToString();
        [Obsolete("Use : "+nameof(AdEventManager)+"."+nameof(AdEventManager.AcquireEvent))]
        protected internal AutoEvent(Action action):this() => this.Action = action;

        public void Release() => AdEventManager.ReleaseEvent(this);

        public void PostEvent() => GameEntry.Ad.ShowRewardedVideoAd(this.EventId);

        public override bool Equals(object obj) =>
            obj == this
            || obj is AutoEvent other
            && Equals(other.EventId, this.EventId);

        public override int GetHashCode() => (EventId.GetHashCode());
    }
}
