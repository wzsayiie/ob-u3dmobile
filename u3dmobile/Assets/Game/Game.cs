using U3DMobile;
using UnityEngine;

class Game : MonoBehaviour
{
    protected void Awake()
    {
#if U3DMOBILE_USE_PUERTS
        JsLauncher.instance.Start();
#endif
    }
}
