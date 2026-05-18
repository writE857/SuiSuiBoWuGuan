//------------------------------------------------------------
// Copyright © 2020-2021 Lefend. All rights reserved.
//------------------------------------------------------------

using UnityEngine;

namespace PublicComponentCenter
{
    public static class LocalizationExtension
    {
        public static void LoadDictionary(this LocalizationComponent localizationComponent, string dictionaryName,
            bool fromBytes, object userData = null)
        {
            if (string.IsNullOrEmpty(dictionaryName))
            {
                Debug.LogWarning("Dictionary name is invalid.");
                return;
            }

            localizationComponent.LoadDictionary(dictionaryName,
                AssetUtility.GetDictionaryAsset(dictionaryName, fromBytes), 0, userData);
        }
    }
}