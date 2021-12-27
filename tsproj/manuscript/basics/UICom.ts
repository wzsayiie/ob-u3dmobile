import { FairyGUI  } from 'csharp'
import { Log       } from './Log'
import { U3DMobile } from 'csharp'

const UIBindKey     = 'UIBind'
const PathSeparator = '.'

enum UIBindType {
    MemberField ,   //set a ui com from resource to the member field.
    ButtonClick ,   //set the method to aGButton.onClick.
    ItemProvider,   //set the method as aGList.itemProvider.
    ItemRenderer,   //set the method as aGList.itemRenderer.
}

interface UIBindEntry {
    propertyKey: string
    bindType   : UIBindType
    searchPath : string
    pathSteps  : string[]
}

export function UIBind(path: string): Function {
    return function (target: object, key: string, desc: PropertyDescriptor): void {
        let clazz = target.constructor

        if (!path || path.match(/^\s*$/)) {
            Log.Error(`property ${clazz.name}.${key} with a empty path`)
            return
        }

        let entry: { [n: string]: UIBindEntry } = clazz[UIBindKey]
        if (!entry) {
            entry = {}
            clazz[UIBindKey] = entry
        }

        let steps = path.split(PathSeparator)
        if (desc) {
            //target[key] should be a method.

            if (steps.length <= 1) {
                Log.Error(`property ${clazz.name}.${key} with a illegal path`)
                return
            }

            let bindType: UIBindType

            let lastStep = steps[steps.length - 1]
            if /**/ (lastStep == 'onClick'     ) { bindType = UIBindType.ButtonClick  }
            else if (lastStep == 'itemProvider') { bindType = UIBindType.ItemProvider }
            else if (lastStep == 'itemRenderer') { bindType = UIBindType.ItemRenderer }
            else {
                Log.Error(`property ${clazz.name}.${key} with a unsupported path`)
                return
            }

            entry[key] = {
                propertyKey: key,
                bindType   : bindType,
                searchPath : path,
                pathSteps  : steps,
            }

        } else {
            //target[key] should be a field.

            entry[key] = {
                propertyKey: key,
                bindType   : UIBindType.MemberField,
                searchPath : path,
                pathSteps  : steps,
            }
        }
    }
}

class UIGObjectNode {
    //NOTE: the top level ui objects of fairy-gui have no names.
    public theGObjectName: string
    public theGObject    : FairyGUI.GObject

    public scanned: boolean

    public childNodes : Map<string, UIGObjectNode>
    public controllers: Map<string, FairyGUI.Controller>
    public transitions: Map<string, FairyGUI.Transition>
}

export class UICom {

    private _rootNode   : UIGObjectNode
    private _boundTarget: object

    constructor(aGCom?: FairyGUI.GComponent, name?: string) {
        this.SetGCom(aGCom, name)
    }

    public SetGCom(aGCom: FairyGUI.GComponent, name?: string): void {
        //NOTE: assignment operation is one-time.
        if (this._rootNode) {
            Log.Error(`can not set ui com repeatedly`)
            return
        }

        if (aGCom) {
            this._rootNode = new UIGObjectNode()
            this._rootNode.theGObjectName = name ?? aGCom.name
            this._rootNode.theGObject = aGCom
        }
    }

    public get theGCom(): FairyGUI.GComponent {
        if (this._rootNode) {
            return this._rootNode.theGObject.asCom
        } else {
            return null
        }
    }

    private AdvanceToNode(steps: string[], end: number): UIGObjectNode {
        let node = this._rootNode
        this.ScanNode(node)

        for (let n = 0; n < end; ++n) {
            if (!node.childNodes) {
                return null
            }

            let step = steps[n]
            if (!node.childNodes.has(step)) {
                return null
            }

            node = node.childNodes.get(step)
            this.ScanNode(node)
        }

        return node
    }

    private ScanNode(node: UIGObjectNode): void {
        if (node.scanned) {
            return
        }
        node.scanned = true

        let com = node.theGObject.asCom
        if (!com) {
            return
        }

        let numChildren = U3DMobile.UIHelper.NumChildrenOf(com)
        if (numChildren > 0) {
            node.childNodes = new Map<string, UIGObjectNode>()

            for (let n = 0; n < numChildren; ++n) {
                let childGObject = com.GetChildAt(n)

                let child = new UIGObjectNode()
                child.theGObjectName = childGObject.name
                child.theGObject = childGObject

                node.childNodes.set(childGObject.name, child)
            }
        }

        let numControllers = U3DMobile.UIHelper.NumControllersOf(com)
        if (numControllers > 0) {
            node.controllers = new Map<string, FairyGUI.Controller>()

            for (let n = 0; n < numControllers; ++n) {
                let controller = com.GetControllerAt(n)
                node.controllers.set(controller.name, controller)
            }
        }

        let numTransitions = U3DMobile.UIHelper.NumTransitionsOf(com)
        if (numTransitions > 0) {
            node.transitions = new Map<string, FairyGUI.Transition>()

            for (let n = 0; n < numTransitions; ++n) {
                let transition = com.GetTransitionAt(n)
                node.transitions.set(transition.name, transition)
            }
        }
    }

    public FindGObject   (path: string) { return <FairyGUI.GObject   > this.Find(path, 'O') }
    public FindController(path: string) { return <FairyGUI.Controller> this.Find(path, 'C') }
    public FindTransition(path: string) { return <FairyGUI.Transition> this.Find(path, 'T') }

    private Find(path: string, targetType: string): object {
        if (!path || path.match(/^\s*$/)) {
            Log.Error(`try to find ui item with a empty path`)
            return null
        }

        if (!this._rootNode) {
            return null
        }

        let rootName = this._rootNode.theGObjectName
        let steps    = path.split(PathSeparator)
        let lastStep = steps[steps.length - 1]

        let node = this.AdvanceToNode(steps, steps.length - 1)
        if (!node) {
            Log.Error(`not found '${path}' in '${rootName}'`)
            return null
        }

        if (targetType == 'O') {
            if (!node.childNodes?.has(lastStep)) {
                Log.Error(`not found ui object '${path}' in '${rootName}'`)
                return null
            }
            return node.childNodes.get(lastStep).theGObject

        } else if (targetType == 'C') {
            if (!node.controllers?.has(lastStep)) {
                Log.Error(`not found controller '${path}' in '${rootName}'`)
                return null
            }
            return node.controllers.get(lastStep)

        } else /* if (targetType == 'T') */ {
            if (!node.transitions?.has(lastStep)) {
                Log.Error(`not found transition '${path}' in '${rootName}'`)
                return null
            }
            return node.transitions.get(lastStep)
        }
    }

    public Bind(target: object): void {
        //NOTE: bind operation is one-time.
        if (this._boundTarget) {
            Log.Error(`can not bind ui com repeatedly`)
            return
        }

        if (!target) {
            Log.Error(`try to bind ui com to a null object`)
            return
        }
        if (!this._rootNode) {
            return
        }

        let entries: { [n: string]: UIBindEntry } = target.constructor[UIBindKey]
        if (!entries) {
            return
        }

        for (let key in entries) {
            let entry = entries[key]
            let steps = entry.pathSteps

            let node = this.AdvanceToNode(steps, steps.length - 1)
            if (!node) {
                Log.Error(`not found '${entry.searchPath}' in '${this._rootNode.theGObjectName}'`)
                continue
            }

            switch (entry.bindType) {
            case UIBindType.MemberField : this.BindMemberField (entry, node, target); break
            case UIBindType.ButtonClick : this.BindButtonClick (entry, node, target); break
            case UIBindType.ItemProvider: this.BindItemProvider(entry, node, target); break
            case UIBindType.ItemRenderer: this.BindItemRenderer(entry, node, target); break
            }
        }
    }

    public BindMemberField(entry: UIBindEntry, parent: UIGObjectNode, target: object): void {
        let rootName = this._rootNode.theGObject
        let lastStep = entry.pathSteps[entry.pathSteps.length - 1]

        if (!parent.childNodes?.has(lastStep)) {
            Log.Error(`not found '${entry.searchPath}' in '${rootName}'`)
            return
        }

        let node  = parent.childNodes.get(lastStep)
        let value = node.theGObject

        target[entry.propertyKey] = value
    }

    public BindButtonClick(entry: UIBindEntry, node: UIGObjectNode, target: object): void {
        let root   = this._rootNode.theGObjectName
        let button = node.theGObject.asButton

        if (!button) {
            Log.Error(`'${entry.searchPath}' in '${root}' is not a button`)
            return
        }

        button.onClick.Set(() => {
            let func = <() => void> target[entry.propertyKey]
            func.call(target)
        })
    }

    public BindItemProvider(entry: UIBindEntry, node: UIGObjectNode, target: object): void {
        let root = this._rootNode.theGObjectName
        let list = node.theGObject.asList

        if (!list) {
            Log.Error(`'${entry.searchPath}' in '${root}' is not a list`)
            return
        }

        list.itemProvider = (index: number): string => {
            let func = <(n: number) => string> target[entry.propertyKey]
            return func.call(target, index)
        }
    }

    public BindItemRenderer(entry: UIBindEntry, node: UIGObjectNode, target: object): void {
        let root = this._rootNode.theGObjectName
        let list = node.theGObject.asList

        if (!list) {
            Log.Error(`'${entry.searchPath}' in '${root}' is not a list`)
            return
        }

        list.itemRenderer = (index: number, item: FairyGUI.GObject): void => {
            let func = <(n: number, i: FairyGUI.GObject) => void> target[entry.propertyKey]
            func.call(target, index, item)
        }
    }
}
