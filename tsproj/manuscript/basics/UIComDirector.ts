import { FairyGUI  } from 'csharp'
import { U3DMobile } from 'csharp'
import { UICom     } from './UICom'

export class UIComDirector
{
    private _filled : boolean
    private _element: FairyGUI.GObject
    private _source : U3DMobile.PackageSource
    private _pkgName: string
    private _resName: string

    private _window : FairyGUI.Window
    private _com    : UICom
    private _created: boolean
    private _shown  : boolean

    public get window(): FairyGUI.Window { return this._window }
    public get com   (): UICom           { return this._com    }
    public get shown (): boolean         { return this._shown  }

    public SetElement(element: FairyGUI.GObject): void
    {
        if (!element)
        {
            return
        }

        //NOTE: cause the director needs to control the life cycle of the ui,
        //it can only be filled once.
        if (this._filled)
        {
            return
        }

        this._filled  = true
        this._element = element
    }

    public SetResource(source: U3DMobile.PackageSource, pkgName: string, resName: string): void
    {
        if (!pkgName || pkgName.match(/^\s*$/))
        {
            return
        }
        if (!resName || resName.match(/^\s*$/))
        {
            return
        }
        if (this._filled)
        {
            return
        }

        this._filled  = true
        this._source  = source
        this._pkgName = pkgName
        this._resName = resName
    }

    public Show(): void
    {
        if (!this._filled)
        {
            return
        }

        if (!this._created)
        {
            //actions:
            if (!this._element)
            {
                this._element = U3DMobile.PackageManager.instance.CreateElement(
                    this._source, this._pkgName, this._resName
                )
            }

            this._window = new FairyGUI.Window()
            this._window.contentPane = this._element.asCom

            this._com = new UICom()
            this._com.SetRootElement(this._element, this._resName)
            this._com.BindOutlets(this)

            //notification.
            this._created = true
            this.OnCreate()
        }
        if (!this._shown)
        {
            //notification.
            this._shown = true
            this.OnShow()

            //actions.
            this._window.Show()
        }
    }

    public Hide(): void
    {
        if (this._shown)
        {
            //actions.
            this._window.Hide()

            //notification.
            this._shown = false
            this.OnHide()
        }
    }

    public Dispose(): void
    {
        if (this._shown)
        {
            //notificaiton.
            this._shown = false
            this.OnHide()
        }
        if (this._created)
        {
            //actions:
            this._com.UnbindOutlets(this)
            this._com = null

            this._window.Dispose()
            this._window = null

            this._element = null

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
