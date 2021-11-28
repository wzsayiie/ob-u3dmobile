//use the menu items "U3DMOBILE/Install XX" to install fairy-gui runtime and puerts,
//and add the macros on the project setting "Scripting Define Symbols".
#if U3DMOBILE_USE_FAIRYGUI
#if U3DMOBILE_USE_PUERTS

using FairyGUI;

namespace U3DMobile
{
    public static class UIHelper
    {
        public static int numChildrenOf   (GComponent com) { return com._children   .Count; }
        public static int numControllersOf(GComponent com) { return com._controllers.Count; }
        public static int numTransitionsOf(GComponent com) { return com._transitions.Count; }
    }
}

#endif
#endif
