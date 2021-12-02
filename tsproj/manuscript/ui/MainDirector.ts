import { FairyGUI      } from 'csharp'
import { Log           } from '../basics/Log'
import { U3DMobile     } from 'csharp'
import { UICom         } from '../basics/UICom'
import { UIComDirector } from '../basics/UIComDirector'
import { UIOutlet      } from '../basics/UICom'
import { UnityEngine   } from 'csharp'

export class MainDirector extends UIComDirector
{
    @UIOutlet('button')
    private _button: FairyGUI.GButton

    @UIOutlet('button.title')
    private _title: FairyGUI.GTextField

    protected OnCreate(): void
    {
        super.OnCreate()
    }
}
