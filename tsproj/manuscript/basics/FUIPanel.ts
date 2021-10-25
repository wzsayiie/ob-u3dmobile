import { FairyGUI  } from 'csharp'
import { U3DMobile } from 'csharp'

export class FUIPanel
{
    //NOTE: these fields need assigning in the constructor of the subclass.
    protected assetFrom  : U3DMobile.FUIAssetFrom
    protected showStyle  : U3DMobile.FUIShowStyle
    protected packageName: string
    protected panelName  : string

    private _handle = new U3DMobile.FUIPanelHandle()

    Open(completion: Function): void
    {
        this._handle.assetFrom   = this.assetFrom
        this._handle.showStyle   = this.showStyle
        this._handle.packageName = this.packageName
        this._handle.panelName   = this.panelName

        this._handle.createAction  = () => this.OnCreate()
        this._handle.showAction    = () => this.OnShow()
        this._handle.hideAction    = () => this.OnHide()
        this._handle.destroyAction = () => this.OnDestroy()

        U3DMobile.FUIManager.instance.Open(this._handle, () =>
        {
            if (completion)
            {
                completion()
            }
        })
    }

    Close(completion: Function): void
    {
        U3DMobile.FUIManager.instance.Close(this._handle, () =>
        {
            if (completion)
            {
                completion()
            }
        })
    }

    protected OnCreate (): void { this.BindControls() }
    protected OnShow   (): void {}
    protected OnHide   (): void {}
    protected OnDestroy(): void {}

    private BindControls(): void
    {
        let controls = new Map<string, FairyGUI.GObject>()

        let panel = this._handle.window.contentPane
        let count = panel.numChildren
        for (let index = 0; index < count; ++index)
        {
            let control = panel.GetChildAt(index)
            controls.set(control.name, control)
        }

        let keys = Object.keys(this)
        for (let key of keys)
        {
            let control = controls.get(key)
            if (control)
            {
                this[key] = control
            }
        }
    }
}
