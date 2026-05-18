using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fix.Editor
{
    public class TaskManager:IDisposable
    {
        private readonly Dictionary<string, Task> ProcessingTasks = new Dictionary<string, Task>();
        private readonly Dictionary<string, Task> DoneTasks = new Dictionary<string, Task>();
        public bool IsTaskDone(string key ,out Task task) => DoneTasks.TryGetValue(key, out task);

        public bool IsTaskProcessing(string key) => ProcessingTasks.ContainsKey(key);
        public bool HasTask(string key) => ProcessingTasks.ContainsKey(key)||DoneTasks.ContainsKey(key);

        public bool AddTask(string key, Task task)
        {
            if (key == null||task == null) return false;
            if (ProcessingTasks.ContainsKey(key)) return false;
            if (task.Status==TaskStatus.Created) task.Start();
            task.ContinueWith(o => SetTaskDone(key));
            ProcessingTasks.Add(key, task);
            return true;
        }

        private void SetTaskDone(string key)
        {
            var task = ProcessingTasks[key];
            ProcessingTasks.Remove(key);
            if (!DoneTasks.ContainsKey(key)) DoneTasks.Add(key, task);
            else DoneTasks[key] = task;
        }

        public bool RemoveTask(string key) => ProcessingTasks.Remove(key)||DoneTasks.Remove(key);

        public void Dispose()
        {
            foreach (var task in DoneTasks.Values) task.Dispose();
        }
    }
}