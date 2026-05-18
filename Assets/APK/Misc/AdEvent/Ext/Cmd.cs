using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using APK;
using PublicComponentCenter;
using UnityEngine;

namespace AdEvent
{
    internal static class CmdManager
    {
        private static readonly IReadOnlyDictionary<string, ICmdExecutor> Executors;

        static CmdManager()
        {
            Debug.Log($"Init {nameof(CmdManager)}");
            try
            {
                foreach (var type in GetSubClassesOf<ICmdInit>())
                {
                    try
                    {
                        ((ICmdInit) Activator.CreateInstance(type)).Init();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            var dictionary = new Dictionary<string, ICmdExecutor>();
            foreach (var type in GetSubClassesOf<ICmdExecutor>())
            {
                try
                {
                    dictionary.Add(type.Name, (ICmdExecutor) Activator.CreateInstance(type));
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }

            Executors = new ReadOnlyDictionary<string, ICmdExecutor>(dictionary);
            
        }

        private static IEnumerable<Type> GetSubClassesOf<T>()
        {
            return AppDomain.CurrentDomain.GetAssemblies().SelectMany(e => e.GetTypes())
                .Where(e => typeof(T).IsAssignableFrom(e) && e != typeof(T) && e.IsClass);
        }

        public const string Split = " ";
        private static readonly string[] Empty = new string[0];

        public static void Execute(string token)
        {
            try
            {
                var indexOf = token.IndexOf(Split, StringComparison.Ordinal);
                if (indexOf < 0)
                {
                    Executors[token].Execute(Empty);
                }
                else
                {
                    Executors[token.Substring(0, indexOf)].Execute(token.Substring(indexOf + 1)
                        .Split(new string[] {Split}, StringSplitOptions.None));
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Invalid token : {token}\n {e}");
            }
        }
    }

    internal class CmdException : System.Exception
    {
        public CmdException()
        {
        }

        public CmdException(string message) : base(message)
        {
        }
    }

    public interface ICmdExecutor
    {
        void Execute(params string[] value);
    }

    public interface ICmdInit
    {
        void Init();
    }

    internal class SetPlatform : ICmdExecutor
    {
        public static event Action<GamePlatform> OnPlatformChanged;
        public void Execute(params string[] value)
        {
            switch (value.Length)
            {
                case 0:
                {
                    Debug.Log(
                        $"Current Platform:{AutoAdSettings.Instance.Platform.ToString()}");
                    break;
                }
                case 1:
                {
                    AutoAdSettings.Instance.Platform = (GamePlatform) Enum.Parse(typeof(GamePlatform), value[0]);
                    OnPlatformChanged?.Invoke(AutoAdSettings.Instance.Platform);
                    break;
                }
                default:
                    throw new CmdException();
            }
        }
    }

    internal class EyePanel : ICmdExecutor
    {
        public void Execute(params string[] value)
        {
            switch (value.Length)
            {
                case 0:
                {
                    Debug.Log(EyeManager.Instance.Enabled);
                    break;
                }
                case 1:
                {
                    EyeManager.Instance.Enabled = bool.Parse(value[0]);
                    break;
                }
                default:
                    throw new CmdException();
            }
        }
    }

    internal class SetOrientation : ICmdExecutor
    {
        public void Execute(params string[] value)
        {
            switch (value.Length)
            {
                case 0:
                {
                    Debug.Log(AutoAdSettings.Instance.Orientation);
                    break;
                }
                case 1:
                {
                    AutoAdSettings.Instance.Orientation = int.Parse(value[0]);
                    break;
                }
                default:
                    throw new CmdException();
            }
        }
    }
	internal class Reward : ICmdExecutor
    {
        public void Execute(params string[] value)
        {
            switch (value.Length)
            {
                case 0:
                {
					Debug.Log($"{GameEntry.Ad.SimulatedRewards}\nTrue 表示直接发放奖励，无激励角标;False 反之");
                    break;
                }
                case 1:
                {
					bool enableReward = bool.Parse(value[0]);
					GameEntry.Ad.SimulatedRewards=enableReward;
					GameEntry.Ad.RewardIconEnable=!enableReward;
                    break;
                }
                default:
                    throw new CmdException();
            }
        }
    }
}