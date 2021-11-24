//use the menu item "U3DMOBILE/Install FairyGUI Runtime" to install the runtime,
//and add "U3DMOBILE_USE_FAIRYGUI" on the project setting "Scripting Define Symbols".
#if U3DMOBILE_USE_FAIRYGUI

using FairyGUI;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace U3DMobile
{
    public class UIOutletAttribute : Attribute
    {
        public string path;

        public UIOutletAttribute(string path)
        {
            this.path = path;
        }
    }

    class UIElementNode
    {
        public GObject element;
        public Dictionary<string, Controller> controllers;
        public Dictionary<string, UIElementNode> childNodes;
    }

    public class UICom
    {
        private UIElementNode _rootNode;

        public UICom(GObject element = null)
        {
            if (element != null)
            {
                SetRootElement(element);
            }
        }

        public void SetRootElement(GObject element)
        {
            if (element != null)
            {
                _rootNode = new UIElementNode
                {
                    element = element
                };
                GenerateNodes(_rootNode);

                BindOutlets(this);
            }
            else
            {
                _rootNode = null;

                UnbindOutlets(this);
            }
        }

        public GObject rootElement
        {
            get { return _rootNode?.element; }
        }

        private void GenerateNodes(UIElementNode node)
        {
            if (!(node.element is GComponent))
            {
                node.controllers = null;
                node.childNodes  = null;
                return;
            }

            GComponent component = node.element.asCom;

            //get controllers.
            if (component.Controllers.Count > 0)
            {
                node.controllers = new Dictionary<string, Controller>();
                for (int n = 0; n < component.Controllers.Count; ++n)
                {
                    Controller controller = component.GetControllerAt(n);
                    node.controllers.Add(controller.name, controller);
                }
            }
            else
            {
                node.controllers = null;
            }

            //get child nodes.
            if (component.numChildren == 0)
            {
                node.childNodes = null;
                return;
            }

            node.childNodes = new Dictionary<string, UIElementNode>();
            for (int n = 0; n < component.numChildren; ++n)
            {
                var child = new UIElementNode
                {
                    element = component.GetChildAt(n)
                };
                GenerateNodes(child);

                node.childNodes.Add(child.element.name, child);
            }
        }

        public void BindOutlets(object target)
        {
            if (target == null)
            {
                return;
            }

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            FieldInfo[] fields = target.GetType().GetFields(flags);
            foreach (FieldInfo field in fields)
            {
                //in general, the length of the "attributes" should be 0 or 1.
                object[] attributes = field.GetCustomAttributes(typeof(UIOutletAttribute), false);
                if (attributes.Length != 1)
                {
                    continue;
                }

                string path  = (attributes[0] as UIOutletAttribute).path;
                object value = FindElement(path);
                if (value == null)
                {
                    value = FindController(path);
                }

                if (value == null)
                {
                    Log.Error("not found ui element or controller for '{0}.{1}' with path '{2}'",
                        target.GetType().Name,
                        field.Name,
                        path
                    );
                    continue;
                }
                if (!field.FieldType.IsAssignableFrom(value.GetType()))
                {
                    Log.Error(
                        "the field '{0}.{1}: {2}' is incompatible with the type '{3}'",
                        target.GetType().Name,
                        field.Name,
                        field.FieldType.Name,
                        value.GetType().Name
                    );
                    continue;
                }

                field.SetValue(target, value);
            }
        }

        public void UnbindOutlets(object target)
        {
            if (target == null)
            {
                return;
            }

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            FieldInfo[] fields = target.GetType().GetFields(flags);
            foreach (FieldInfo field in fields)
            {
                object[] attributes = field.GetCustomAttributes(typeof(UIOutletAttribute), false);
                if (attributes.Length == 1)
                {
                    field.SetValue(target, null);
                }
            }
        }

        public GObject FindElement(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            string[] steps = path.Split('.');

            UIElementNode node = FindNode(steps, 0, steps.Length);
            return node?.element;
        }

        public Controller FindController(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            string[] steps = path.Split('.');

            UIElementNode node = FindNode(steps, 0, steps.Length - 1);
            if (node != null)
            {
                string lastStep = steps[steps.Length - 1];
                if (node.controllers.ContainsKey(lastStep))
                {
                    return node.controllers[lastStep];
                }
            }
            return null;
        }

        private UIElementNode FindNode(string[] steps, int begin, int end)
        {
            if (_rootNode == null)
            {
                return null;
            }

            UIElementNode node = _rootNode;

            for (int n = begin; n < end; ++n)
            {
                if (node.childNodes == null)
                {
                    return null;
                }

                string step = steps[n];
                if (node.childNodes.ContainsKey(step))
                {
                    node = node.childNodes[step];
                }
                else
                {
                    return null;
                }
            }

            return node;
        }
    }
}

#endif
