//------------------------------------------------------------
// Copyright © 2020-2021 Lefend. All rights reserved.
//------------------------------------------------------------


using PublicComponentCenter;
using UnityEngine;

public class GameScene : MonoBehaviour
{
    // Use this for initialization
    void Start()
    {
        //需要在每个场景里挂的脚本中的Start中挂载的脚本
        GameEntry.Antiaddiction.ShowLoadingPanel(false);
        GameEntry.Antiaddiction.CanvasLogEnable(false);
    }
}