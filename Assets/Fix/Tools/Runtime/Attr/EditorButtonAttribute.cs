using System;
using UnityEngine;

namespace Fix
{
    [AttributeUsage(AttributeTargets.Method)]
    public class EditorButtonAttribute:PropertyAttribute
    {
        public string Name { get; set; }

        public EditorButtonAttribute(string name)
        {
            Name = name;
        }
    }
}