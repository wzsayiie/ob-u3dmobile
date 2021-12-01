//use the menu item "U3DMOBILE/Install FairyGUI Runtime" to install the runtime,
//and add "U3DMOBILE_USE_FAIRYGUI" on the project setting "Scripting Define Symbols".
#if U3DMOBILE_USE_FAIRYGUI

using FairyGUI;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace U3DMobile
{
    class UIOutletAttribute : Attribute
    {
        public string path;

        public UIOutletAttribute(string path)
        {
            this.path = path;
        }
    }

    class UIOutletFieldItem
    {
        public const char PathSeperator = '.';

        public FieldInfo fieldInfo;
        public string[]  pathSteps;
    }

    class UIOutletFieldMap : Singleton<UIOutletFieldMap>
    {
        public static UIOutletFieldMap instance { get { return GetInstance(); } }

        private Dictionary<Type, UIOutletFieldItem[]> _fieldMap;

        public UIOutletFieldItem[] GetFields(Type type)
        {
            //null type.
            if (type == null)
            {
                return null;
            }

            //get items from caches.
            if (_fieldMap == null)
            {
                _fieldMap = new Dictionary<Type, UIOutletFieldItem[]>();
            }
            if (_fieldMap.ContainsKey(type))
            {
                return _fieldMap[type];
            }

            //add new items.
            UIOutletFieldItem[] items = CollectFields(type);
            _fieldMap.Add(type, items);
            return items;
        }

        private UIOutletFieldItem[] CollectFields(Type type)
        {
            var list = new List<UIOutletFieldItem>();

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            FieldInfo[] fields = type.GetFields(flags);
            foreach (FieldInfo field in fields)
            {
                //in general, the length of the "attributes" should be 0 or 1.
                object[] attributes = field.GetCustomAttributes(typeof(UIOutletAttribute), false);
                if (attributes.Length != 1)
                {
                    continue;
                }

                string path = (attributes[0] as UIOutletAttribute).path;
                if (string.IsNullOrWhiteSpace(path))
                {
                    Log.Error("outlet '{0}.{1}' with a empty path", type.Name, field.Name);
                    continue;
                }

                string[] steps = path.Split(UIOutletFieldItem.PathSeperator);
                list.Add(new UIOutletFieldItem
                {
                    fieldInfo = field,
                    pathSteps = steps,
                });
            }

            return list.ToArray();
        }
    }

    class UIElementNode
    {
        //NOTE: the top level ui objects of fairy-gui have no names.
        public string  elementName;
        public GObject element;

        public bool scanned;

        public Dictionary<string, UIElementNode> childNodes ;
        public Dictionary<string, Controller   > controllers;
        public Dictionary<string, Transition   > transitions;
    }

    class UICom
    {
        private UIElementNode _rootNode;

        public void SetRootElement(GObject element, string name = null)
        {
            if (_rootNode != null)
            {
                UnbindOutlets(this);
                _rootNode = null;
            }

            //NOTE: it must be re-bound,
            //regardless of whether the element is the same as the current element.
            //because the ui tree may change.
            if (element != null)
            {
                _rootNode = new UIElementNode
                {
                    elementName = name ?? element.name,
                    element = element,
                };
                BindOutlets(this);
            }
        }

        public GObject rootElement
        {
            get { return _rootNode?.element; }
        }

        public void BindOutlets(object target)
        {
            if (target == null)
            {
                return;
            }
            if (_rootNode == null)
            {
                return;
            }

            UIOutletFieldItem[] fieldItems = UIOutletFieldMap.instance.GetFields(target.GetType());
            foreach (UIOutletFieldItem item in fieldItems)
            {
                object value = FindValue(item.pathSteps);
                if (value == null)
                {
                    Log.Error("from '{0}', not found any suitable object for '{1}.{2}'",
                        _rootNode.elementName,
                        target.GetType().Name,
                        item.fieldInfo.Name
                    );
                    continue;
                }

                if (!item.fieldInfo.FieldType.IsAssignableFrom(value.GetType()))
                {
                    Log.Error(
                        "the field '{0}.{1}: {2}' is incompatible with the type '{3}'",
                        target.GetType().Name,
                        item.fieldInfo.Name,
                        item.fieldInfo.FieldType.Name,
                        value.GetType().Name
                    );
                    continue;
                }

                item.fieldInfo.SetValue(target, value);
            }
        }

        public void UnbindOutlets(object target)
        {
            if (target == null)
            {
                return;
            }

            UIOutletFieldItem[] fieldItems = UIOutletFieldMap.instance.GetFields(target.GetType());
            foreach (UIOutletFieldItem item in fieldItems)
            {
                item.fieldInfo.SetValue(target, null);
            }
        }

        private object FindValue(string[] steps)
        {
            UIElementNode node = ExpandNode(steps, steps.Length - 1);
            if (node == null)
            {
                return null;
            }

            string lastStep = steps[steps.Length - 1];
            if (node.childNodes != null && node.childNodes.ContainsKey(lastStep))
            {
                return node.childNodes[lastStep].element;
            }
            if (node.controllers != null && node.controllers.ContainsKey(lastStep))
            {
                return node.controllers[lastStep];
            }
            if (node.transitions != null && node.transitions.ContainsKey(lastStep))
            {
                return node.transitions[lastStep];
            }

            return null;
        }

        private UIElementNode ExpandNode(string[] steps, int end)
        {
            UIElementNode node = _rootNode;
            ScanNode(node);

            for (int n = 0; n < end; ++n)
            {
                if (node.childNodes == null)
                {
                    return null;
                }

                string step = steps[n];
                if (!node.childNodes.ContainsKey(step))
                {
                    return null;
                }

                node = node.childNodes[step];
                ScanNode(node);
            }

            return node;
        }

        private void ScanNode(UIElementNode node)
        {
            if (node.scanned)
            {
                return;
            }

            GComponent com = node.element.asCom;
            if (com == null)
            {
                return;
            }

            if (com._children != null && com._children.Count > 0)
            {
                node.childNodes = new Dictionary<string, UIElementNode>();
                foreach (GObject element in com._children)
                {
                    var child = new UIElementNode
                    {
                        elementName = element.name,
                        element = element,
                    };
                    node.childNodes.Add(child.element.name, child);
                }
            }

            if (com._controllers != null && com._controllers.Count > 0)
            {
                node.controllers = new Dictionary<string, Controller>();
                foreach (Controller controller in com._controllers)
                {
                    node.controllers.Add(controller.name, controller);
                }
            }

            if (com._transitions != null && com._transitions.Count > 0)
            {
                node.transitions = new Dictionary<string, Transition>();
                foreach (Transition transition in com._transitions)
                {
                    node.transitions.Add(transition.name, transition);
                }
            }

            node.scanned = true;
        }

        public GObject FindElement(string path)
        {
            UIElementNode node = FindParentNode(path, out string lastStep);
            if (node == null)
            {
                return null;
            }

            if (node.childNodes != null && node.childNodes.ContainsKey(lastStep))
            {
                return node.childNodes[lastStep].element;
            }
            else
            {
                return null;
            }
        }

        public Controller FindController(string path)
        {
            UIElementNode node = FindParentNode(path, out string lastStep);
            if (node == null)
            {
                return null;
            }

            if (node.controllers != null && node.controllers.ContainsKey(lastStep))
            {
                return node.controllers[lastStep];
            }
            else
            {
                return null;
            }
        }

        public Transition FindTransition(string path)
        {
            UIElementNode node = FindParentNode(path, out string lastStep);
            if (node == null)
            {
                return null;
            }

            if (node.transitions != null && node.transitions.ContainsKey(lastStep))
            {
                return node.transitions[lastStep];
            }
            else
            {
                return null;
            }
        }

        private UIElementNode FindParentNode(string path, out string lastStep)
        {
            lastStep = null;

            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }
            if (_rootNode == null)
            {
                return null;
            }

            string[] steps = path.Split(UIOutletFieldItem.PathSeperator);
            lastStep = steps[steps.Length - 1];

            return ExpandNode(steps, steps.Length - 1);
        }
    }
}

#endif
