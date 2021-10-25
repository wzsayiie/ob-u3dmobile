import { FairyGUI  } from "csharp"
import { FUIPanel  } from "../basics/FUIPanel"

export class MainUIPanel extends FUIPanel
{
    constructor()
    {
        super()

        this.packageName = "mainui"
        this.panelName   = "MainUIPanel"
    }

    protected OnCreate(): void
    {
        super.OnCreate()
    }
}
