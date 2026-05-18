using System;
using UnityEngine;

namespace Fix
{
    [AttributeUsage(AttributeTargets.Field)]
    public class DisplayAttribute : PropertyAttribute
    {
        public string TargetFieldName { get; }
        public string TargetFieldValue { get; }
        public bool Operator { get; set; } = true;
 
        public DisplayAttribute(string targetFieldName, string targetFieldValue)
        {
            this.TargetFieldName = targetFieldName;
            this.TargetFieldValue = targetFieldValue;
        }
    }
}