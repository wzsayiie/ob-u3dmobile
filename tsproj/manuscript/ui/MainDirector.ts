import { FairyGUI      } from 'csharp'
import { Log           } from '../basics/Log'
import { UIComDirector } from '../basics/UIComDirector'

export class MainDirector extends UIComDirector
{
    protected static outlets =
    {
        _button: 'button',
        _title : 'button.title',
    }

    private _button: FairyGUI.GButton
    private _title : FairyGUI.GTextField

    protected OnCreate(): void
    {
        super.OnCreate()
    }
}
