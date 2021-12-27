import { FairyGUI      } from 'csharp'
import { Log           } from '../basics/Log'
import { U3DMobile     } from 'csharp'
import { UIBind        } from '../basics/UICom'
import { UICom         } from '../basics/UICom'
import { UIComDirector } from '../basics/UIComDirector'
import { UnityEngine   } from 'csharp'

export class MainDirector extends UIComDirector {

    @UIBind('button')
    private _button: FairyGUI.GButton

    protected OnCreate(): void {
        super.OnCreate()
    }

    @UIBind('button.onClick')
    protected OnButtonClick(): void {
    }
}
