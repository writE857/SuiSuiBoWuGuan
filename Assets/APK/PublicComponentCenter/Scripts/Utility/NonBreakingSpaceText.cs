//------------------------------------------------------------
// Copyright © 2020-2021 Lefend. All rights reserved.
//------------------------------------------------------------


using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class NonBreakingSpaceText : MonoBehaviour
{
    public static readonly string no_breaking_space = "\u00A0";

    protected Text mytext;

    void Start()
    {
        mytext = this.GetComponent<Text>();
        mytext.RegisterDirtyVerticesCallback(SetMyText);
    }

    public void SetMyText()
    {
        if (mytext.text.Contains(" "))
        {
            mytext.text = mytext.text.Replace(" ", no_breaking_space);
        }
    }
}