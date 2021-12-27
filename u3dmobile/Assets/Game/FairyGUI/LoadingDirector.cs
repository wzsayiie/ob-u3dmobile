//use the menu item "U3DMOBILE/Install FairyGUI Runtime" to install the runtime,
//and add "U3DMOBILE_USE_FAIRYGUI" on the project setting "Scripting Define Symbols".
#if U3DMOBILE_USE_FAIRYGUI

using FairyGUI;

namespace U3DMobile
{
    class LoadingDirector : UIComDirector
    {
        [UIBind("button")]
        private GButton _button;

        protected override void OnCreate()
        {
            base.OnCreate();
        }

        [UIBind("button.onClick")]
        protected void OnButtonClick()
        {
        }
    }
}

#endif
