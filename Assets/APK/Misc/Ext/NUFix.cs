using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[DisallowMultipleComponent]
public class NUFix : MonoBehaviour
{
    private static readonly Type UICameraType;
    private static readonly HashSet<MonoBehaviour> Components = new HashSet<MonoBehaviour>();
    private static int count;

    static NUFix()
    {
        UICameraType = AppDomain
            .CurrentDomain
            .GetAssemblies()
            .Select(e => e.GetType("UICamera"))
            .FirstOrDefault(e => e != null);
    }


    private void OnEnable()
    {
        if (UICameraType == null) return;
        count++;
        var uiCameras = FindObjectsOfType(UICameraType).OfType<MonoBehaviour>().ToArray();
        foreach (var uiCamera in uiCameras)
        {
            uiCamera.enabled = false;
            Components.Add(uiCamera);
        }
    }

    private void OnDisable()
    {
        if (UICameraType == null) return;
        if (--count != 0) return;
        foreach (var uiCamera in Components.Where(e => e != null)) uiCamera.enabled = true;
        Components.Clear();
    }
}