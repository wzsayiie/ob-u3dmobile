import { FairyGUI  } from 'csharp'
import { Log       } from './Log'
import { U3DMobile } from 'csharp'
import { UICom     } from './UICom'

export class UIComDirector {

    private _filled   : boolean
    private _theGCom  : FairyGUI.GComponent
    private _pkgSource: U3DMobile.PackageSource
    private _pkgName  : string
    private _comName  : string

    private _window : FairyGUI.Window
    private _com    : UICom
    private _created: boolean
    private _shown  : boolean

    public get window(): FairyGUI.Window { return this._window }
    public get com   (): UICom           { return this._com    }
    public get shown (): boolean         { return this._shown  }

    public SetGCom(aGCom: FairyGUI.GComponent): void {
        if (this._filled) {
            Log.Error(`can not set ui com repeatedly`)
            return
        }

        if (aGCom) {
            this._filled  = true
            this._theGCom = aGCom
        }
    }

    public SetResource(source: U3DMobile.PackageSource, pkgName: string, comName: string): void {
        if (this._filled) {
            Log.Error(`can not set package resource repeatedly`)
            return
        }

        if (!pkgName || pkgName.match(/^\s*$/)) {
            Log.Error(`try to set a empty package name`)
            return
        }
        if (!comName || comName.match(/^\s*$/)) {
            Log.Error(`try to set a empty component name`)
            return
        }

        this._filled    = true
        this._pkgSource = source
        this._pkgName   = pkgName
        this._comName   = comName
    }

    public Show(): void {
        if (!this._filled) {
            return
        }

        if (!this._created) {
            //actions:
            if (!this._theGCom) {
                let manager = U3DMobile.PackageManager.instance
                let aGObject = manager.CreateGObject(this._pkgSource, this._pkgName, this._comName)

                this._theGCom = aGObject.asCom
                if (!this._theGCom) {
                    Log.Error(`failed to create '${this._pkgName}/${this._comName}'`)
                    return
                }
            }

            this._window = new FairyGUI.Window()
            this._window.contentPane = this._theGCom

            this._com = new UICom()
            this._com.SetGCom(this._theGCom, this._comName)
            this._com.Bind(this)

            //notification.
            this._created = true
            this.OnCreate()
        }
        if (!this._shown) {
            //notification.
            this._shown = true
            this.OnShow()

            //actions.
            this._window.Show()
        }
    }

    public Hide(): void {
        if (this._shown) {
            //actions.
            this._window.Hide()

            //notification.
            this._shown = false
            this.OnHide()
        }
    }

    public Dispose(): void {
        if (this._shown) {
            //notificaiton.
            this._shown = false
            this.OnHide()
        }
        if (this._created) {
            //actions:
            this._com = null

            this._window.Dispose()
            this._window = null

            this._theGCom = null

            //notification.
            this._created = false
            this.OnDestroy()
        }
    }

    protected OnCreate (): void {}
    protected OnShow   (): void {}
    protected OnHide   (): void {}
    protected OnDestroy(): void {}
}
