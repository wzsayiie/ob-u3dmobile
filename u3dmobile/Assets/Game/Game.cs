using U3DMobile;
using UnityEngine;

class Game : MonoBehaviour
{
    protected void Awake()
    {
        JsLauncher.instance.Start();
    }
}
