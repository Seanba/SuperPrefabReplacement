using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AppearingBlock : MonoBehaviour
{
    public int m_GroupNumber;

#if UNITY_EDITOR
    // Only allow editor scripts to set our group number property
    // This is going to be set by custom properties from Tiled Objects in our Tiled map file
    public int GroupNumber
    {
        set
        {
            m_GroupNumber = value;
        }
    }
#endif
}

