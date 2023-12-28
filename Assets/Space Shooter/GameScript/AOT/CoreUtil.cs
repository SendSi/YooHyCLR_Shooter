using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoreUtil : MonoBehaviour
{
    public static CoreUtil Instance;
    void Awake()
    {
        Instance = this;
    }


}
