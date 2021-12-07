//use the menu items "U3DMOBILE/Install XX" to install fairy-gui runtime and puerts,
//and add the macros on the project setting "Scripting Define Symbols".
#if U3DMOBILE_USE_FAIRYGUI && U3DMOBILE_USE_PUERTS

using FairyGUI;

namespace U3DMobile
{
    public static class UIHelper
    {
        public static int NumChildrenOf   (GComponent com) { return com._children   .Count; }
        public static int NumControllersOf(GComponent com) { return com._controllers.Count; }
        public static int NumTransitionsOf(GComponent com) { return com._transitions.Count; }
    }
}

#endif
