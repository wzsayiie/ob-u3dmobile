using System;
using System.Collections.Generic;

namespace U3DMobile
{
    public class Scheduler : SingletonBehaviour<Scheduler>
    {
        public static Scheduler instance { get { return GetInstance(); } }

        //NOTE: the value of DateTime.Now.Ticks may be very large, beyond the range of a float.
        private long _tickMillisecondsBase = 0;

        public float tickSeconds()
        {
            long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

            if (_tickMillisecondsBase == 0)
            {
                _tickMillisecondsBase = milliseconds;
            }

            return (milliseconds - _tickMillisecondsBase) / 1000;
        }

        private class ActionConfig
        {
            public bool  runOnce  = false;
            public float nextTick = 0;
            public float interval = 0;
        }

        private Dictionary<Action, ActionConfig> _actions = new Dictionary<Action, ActionConfig>();

        public Action RunAfterSeconds(float seconds, Action action)
        {
            if (action == null)
            {
                return null;
            }

            ActionConfig config = new ActionConfig()
            {
                runOnce  = true,
                nextTick = tickSeconds() + seconds,
            };
            _actions.Add(action, config);

            return action;
        }

        public Action RunEverySeconds(float seconds, Action action)
        {
            if (action == null)
            {
                return null;
            }

            ActionConfig config = new ActionConfig()
            {
                runOnce  = false,
                nextTick = tickSeconds() + seconds,
                interval = seconds,
            };
            _actions.Add(action, config);

            return action;
        }

        public void CancelAction(Action action)
        {
            _actions.Remove(action);
        }

        protected void FixedUpdate()
        {
            //NOTE: create a copy of "_actions" to iterate,
            //it will be modified possibly during traversal.
            var actions = new Dictionary<Action, ActionConfig>(_actions);

            float tick = tickSeconds();
            foreach (var pair in actions)
            {
                ActionConfig config = pair.Value;
                if (config.nextTick > tick)
                {
                    continue;
                }

                Action action = pair.Key;
                pair.Key();

                if (config.runOnce)
                {
                    _actions.Remove(action);
                }
                else
                {
                    config.nextTick += config.interval;
                }
            }
        }
    }
}
