import { FairyGUI  } from 'csharp'
import { Log       } from './Log'
import { U3DMobile } from 'csharp'

const OutletsKey    = 'OUTLETS'
const PathSeparator = '.'

export function UIOutlet(path: string)
{
    return function (target: Object, key: string): void
    {
        let cls = target.constructor

        if (!cls[OutletsKey])
        {
            cls[OutletsKey] = {}
        }
        cls[OutletsKey][key] = path
    }
}

class UIElementNode
{
    //NOTE: the top level ui objects of fairy-gui have no names.
    public elementName: string
    public element    : FairyGUI.GObject

    public scanned: boolean

    public childNodes : Map<string, UIElementNode>
    public controllers: Map<string, FairyGUI.Controller>
    public transitions: Map<string, FairyGUI.Transition>
}

export class UICom
{
    private _rootNode: UIElementNode

    public SetRootElement(element: FairyGUI.GObject, name?: string): void
    {
        if (this._rootNode)
        {
            this.UnbindOutlets(this)
            this._rootNode = null
        }

        //NOTE: it must be re-bound,
        //regardless of whether the element is the same as the current element.
        //because the ui tree may change.
        if (element)
        {
            this._rootNode = new UIElementNode()
            this._rootNode.elementName = name ?? element.name
            this._rootNode.element = element

            this.BindOutlets(this)
        }
    }

    public get rootElement(): FairyGUI.GObject
    {
        return this._rootNode?.element
    }

    public BindOutlets(target: Object): void
    {
        if (!target || !this._rootNode)
        {
            return
        }

        let fields = this.GetOutletFields(target)
        if (!fields)
        {
            return
        }

        for (let name in fields)
        {
            let path = fields[name]
            if (!path || path.match(/^\s*$/))
            {
                Log.Error(
                    `outlet "${target.constructor.name}.${name}" with a empty path`
                )
                continue
            }

            let value = this.FindValue(path)
            if (!value)
            {
                Log.Error(
                    `from "${this._rootNode.elementName}", ` +
                    `not found any suitable ui object for ` +
                    `"${target.constructor.name}.${name}"`
                )
                continue
            }

            target[name] = value
        }
    }

    public UnbindOutlets(target: Object): void
    {
        if (!target)
        {
            return
        }

        let fields = this.GetOutletFields(target)
        if (!fields)
        {
            return
        }

        for (let name in fields)
        {
            target[name] = null
        }
    }

    private GetOutletFields(target: Object): { [index: string]: string }
    {
        if (target.constructor)
        {
            return target.constructor[OutletsKey]
        }
        else
        {
            return null
        }
    }

    private FindValue(path: string): any
    {
        let steps = path.split(PathSeparator)

        let node = this.ExpandNode(steps, steps.length - 1)
        if (!node)
        {
            return null
        }

        let lastStep = steps[steps.length - 1]
        if (node.childNodes && node.childNodes.has(lastStep))
        {
            return node.childNodes.get(lastStep).element
        }
        if (node.controllers && node.controllers.has(lastStep))
        {
            return node.controllers.get(lastStep)
        }
        if (node.transitions && node.transitions.get(lastStep))
        {
            return node.transitions.get(lastStep)
        }

        return null
    }

    private ExpandNode(steps: string[], end: number): UIElementNode
    {
        let node = this._rootNode
        this.ScanNode(node)

        for (let n = 0; n < end; ++n)
        {
            if (!node.childNodes)
            {
                return null
            }

            let step = steps[n]
            if (!node.childNodes.has(step))
            {
                return null
            }

            node = node.childNodes.get(step)
            this.ScanNode(node)
        }

        return node
    }

    private ScanNode(node: UIElementNode): void
    {
        if (node.scanned)
        {
            return
        }

        let com = node.element.asCom
        if (!com)
        {
            return
        }

        let numChildren = U3DMobile.UIHelper.numChildrenOf(com)
        if (numChildren > 0)
        {
            node.childNodes = new Map<string, UIElementNode>()
            for (let n = 0; n < numChildren; ++n)
            {
                let element = com.GetChildAt(n)

                let child = new UIElementNode()
                child.elementName = element.name
                child.element = element

                node.childNodes.set(element.name, child)
            }
        }

        let numControllers = U3DMobile.UIHelper.numControllersOf(com)
        if (numControllers > 0)
        {
            node.controllers = new Map<string, FairyGUI.Controller>()
            for (let n = 0; n < numControllers; ++n)
            {
                let controller = com.GetControllerAt(n)
                node.controllers.set(controller.name, controller)
            }
        }

        let numTransitions = U3DMobile.UIHelper.numTransitionsOf(com)
        if (numTransitions > 0)
        {
            node.transitions = new Map<string, FairyGUI.Transition>()
            for (let n = 0; n < numTransitions; ++n)
            {
                let transition = com.GetTransitionAt(n)
                node.transitions.set(transition.name, transition)
            }
        }

        node.scanned = true
    }

    public FindElement(path: string): FairyGUI.GObject
    {
        let last = { value: '' }
        let node = this.FindParentNode(path, last)

        if (node && node.childNodes && node.childNodes.has(last.value))
        {
            return node.childNodes.get(last.value).element
        }
        else
        {
            return null
        }
    }

    public FindController(path: string): FairyGUI.Controller
    {
        let last = { value: '' }
        let node = this.FindParentNode(path, last)

        if (node && node.controllers && node.controllers.has(last.value))
        {
            return node.controllers.get(last.value)
        }
        else
        {
            return null
        }
    }

    public FindTransition(path: string): FairyGUI.Transition
    {
        let last = { value: '' }
        let node = this.FindParentNode(path, last)

        if (node && node.transitions && node.transitions.has(last.value))
        {
            return node.transitions.get(last.value)
        }
        else
        {
            return null
        }
    }

    private FindParentNode(path: string, lastStep: { value: string }): UIElementNode
    {
        if (!path || path.match(/^\s*$/))
        {
            return null
        }
        if (!this._rootNode)
        {
            return null
        }

        let steps = path.split(PathSeparator)
        lastStep.value = steps[steps.length - 1]

        return this.ExpandNode(steps, steps.length - 1)
    }
}
