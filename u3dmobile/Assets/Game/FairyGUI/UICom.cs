//use the menu item "U3DMOBILE/Install FairyGUI Runtime" to install the runtime,
//and add "U3DMOBILE_USE_FAIRYGUI" on the project setting "Scripting Define Symbols".
#if U3DMOBILE_USE_FAIRYGUI

using FairyGUI;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace U3DMobile
{
    class UIBindAttribute : Attribute
    {
        public string path;

        public UIBindAttribute(string path)
        {
            this.path = path;
        }
    }

    enum UIBindType
    {
        MemberField ,   //set a ui com from resource to the member field.
        ButtonClick ,   //set the method to aGButton.onClick.
        ItemProvider,   //set the method as aGList.itemProvider.
        ItemRenderer,   //set the method as aGList.itemRenderer.
    }

    class UIBindEntry
    {
        public const char PathSeperator = '.';

        public string     searchPath;
        public string[]   pathSteps ;
        public UIBindType bindType  ;
        public FieldInfo  fieldInfo ;
        public MethodInfo methodInfo;
    }

    class UIBindMap : Singleton<UIBindMap>
    {
        public static UIBindMap instance { get { return GetInstance(); } }

        private Dictionary<Type, UIBindEntry[]> _caches;

        public UIBindEntry[] GetEntries(Type type)
        {
            //null type.
            if (type == null)
            {
                return null;
            }

            //get entries from caches.
            if (_caches == null)
            {
                _caches = new Dictionary<Type, UIBindEntry[]>();
            }
            if (_caches.ContainsKey(type))
            {
                return _caches[type];
            }

            //add new entry.
            UIBindEntry[] entries = CollectEntries(type);
            _caches.Add(type, entries);
            return entries;
        }

        private UIBindEntry[] CollectEntries(Type type)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var list = new List<UIBindEntry>();

            FieldInfo[] fields = type.GetFields(flags);
            foreach (FieldInfo field in fields)
            {
                TryAddFieldEntry(list, type, field);
            }

            MethodInfo[] methods = type.GetMethods(flags);
            foreach (MethodInfo method in methods)
            {
                TryAddMethodEntry(list, type, method);
            }

            return list.ToArray();
        }

        private void TryAddFieldEntry(List<UIBindEntry> list, Type classType, FieldInfo field)
        {
            //in general, the length of the "attributes" should be 0 or 1.
            object[] attributes = field.GetCustomAttributes(typeof(UIBindAttribute), false);
            if (attributes.Length != 1)
            {
                return;
            }

            string path = (attributes[0] as UIBindAttribute).path;
            if (string.IsNullOrWhiteSpace(path))
            {
                Log.Error($"field '{classType.Name}.{field.Name}' with a empty path");
                return;
            }

            list.Add(new UIBindEntry
            {
                searchPath = path,
                pathSteps  = path.Split(UIBindEntry.PathSeperator),
                bindType   = UIBindType.MemberField,
                fieldInfo  = field,
                methodInfo = null,
            });
        }

        private void TryAddMethodEntry(List<UIBindEntry> list, Type classType, MethodInfo method)
        {
            object[] attributes = method.GetCustomAttributes(typeof(UIBindAttribute), false);
            if (attributes.Length != 1)
            {
                return;
            }

            string path = (attributes[0] as UIBindAttribute).path;
            if (string.IsNullOrWhiteSpace(path))
            {
                Log.Error($"method '{classType.Name}.{method.Name}' with a empty path");
                return;
            }

            string[] steps = path.Split(UIBindEntry.PathSeperator);
            if (steps.Length <= 1)
            {
                Log.Error($"method '{classType.Name}.{method.Name}' with a illogical path");
                return;
            }
            
            string lastStep = steps[steps.Length - 1];
            
            UIBindType bindType;
            if (lastStep == "onClick")
            {
                bindType = UIBindType.ButtonClick;
                if (!IsMethodMeet(method, typeof(void)))
                {
                    Log.Error($"method '{classType.Name}.{method.Name}' can not respond button click");
                    return;
                }
            }
            else if (lastStep == "itemProvider")
            {
                bindType = UIBindType.ItemProvider;
                if (!IsMethodMeet(method, typeof(string), typeof(int)))
                {
                    Log.Error($"method '{classType.Name}.{method.Name}' can not as a list provider");
                    return;
                }
            }
            else if (lastStep == "itemRenderer")
            {
                bindType = UIBindType.ItemRenderer;
                if (!IsMethodMeet(method, typeof(void), typeof(int), typeof(GObject)))
                {
                    Log.Error($"method '{classType.Name}.{method.Name}' can not as a list renderer");
                    return;
                }
            }
            else
            {
                Log.Error($"method '{classType.Name}.{method.Name}' with unsupported path");
                return;
            }
            
            list.Add(new UIBindEntry
            {
                searchPath = path,
                pathSteps  = steps,
                bindType   = bindType,
                fieldInfo  = null,
                methodInfo = method,
            });
        }

        private bool IsMethodMeet(MethodInfo method, Type ret, params Type[] args)
        {
            if (method.ReturnType != ret)
            {
                return false;
            }

            ParameterInfo[] funcArgs = method.GetParameters();
            if (funcArgs.Length != args.Length)
            {
                return false;
            }

            for (int n = 0; n < funcArgs.Length; ++n)
            {
                if (funcArgs[n].ParameterType != args[n])
                {
                    return false;
                }
            }

            return true;
        }
    }

    class UIGObjectNode
    {
        //NOTE: the top level ui objects of fairy-gui have no names.
        public string  theGObjectName;
        public GObject theGObject;

        public bool scanned;

        public Dictionary<string, UIGObjectNode> childNodes ;
        public Dictionary<string, Controller   > controllers;
        public Dictionary<string, Transition   > transitions;
    }

    class UICom
    {
        private UIGObjectNode _rootNode;
        private object _boundTarget;

        public UICom(GComponent aGCom = null, string name = null)
        {
            SetGCom(aGCom, name);
        }

        public void SetGCom(GComponent aGCom, string name = null)
        {
            //NOTE: assignment operation is one-time.
            if (_rootNode != null)
            {
                Log.Error($"can not set ui com repeatedly");
                return;
            }

            if (aGCom != null)
            {
                _rootNode = new UIGObjectNode
                {
                    theGObjectName = name ?? aGCom.name,
                    theGObject = aGCom,
                };
            }
        }

        public GComponent theGCom
        {
            get { return _rootNode?.theGObject.asCom; }
        }

        private UIGObjectNode AdvanceToNode(string[] steps, int end)
        {
            UIGObjectNode node = _rootNode;
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

        private void ScanNode(UIGObjectNode node)
        {
            if (node.scanned)
            {
                return;
            }
            node.scanned = true;

            GComponent com = node.theGObject.asCom;
            if (com == null)
            {
                return;
            }

            if (com._children?.Count > 0)
            {
                node.childNodes = new Dictionary<string, UIGObjectNode>();
                foreach (GObject child in com._children)
                {
                    var childNode = new UIGObjectNode
                    {
                        theGObjectName = child.name,
                        theGObject = child,
                    };
                    node.childNodes.Add(child.name, childNode);
                }
            }

            if (com._controllers?.Count > 0)
            {
                node.controllers = new Dictionary<string, Controller>();
                foreach (Controller controller in com._controllers)
                {
                    node.controllers.Add(controller.name, controller);
                }
            }

            if (com._transitions?.Count > 0)
            {
                node.transitions = new Dictionary<string, Transition>();
                foreach (Transition transition in com._transitions)
                {
                    node.transitions.Add(transition.name, transition);
                }
            }
        }

        public GObject    FindGObject   (string path) { return Find(path, 'O') as GObject   ; }
        public Controller FindController(string path) { return Find(path, 'C') as Controller; }
        public Transition FindTransition(string path) { return Find(path, 'T') as Transition; }

        private object Find(string path, char targetType)
        {
            if (string.IsNullOrEmpty(path))
            {
                Log.Error($"try to find ui item with a empty path");
                return null;
            }

            if (_rootNode == null)
            {
                return null;
            }

            string   rootName = _rootNode.theGObjectName;
            string[] steps    = path.Split(UIBindEntry.PathSeperator);
            string   lastStep = steps[steps.Length - 1];

            UIGObjectNode node = AdvanceToNode(steps, steps.Length - 1);
            if (node == null)
            {
                Log.Error($"not found '{path}' in '{rootName}'");
                return null;
            }

            if (targetType == 'O')
            {
                if ((bool) !node.childNodes?.ContainsKey(lastStep))
                {
                    Log.Error($"not found ui object '{path}' in '{rootName}'");
                    return null;
                }

                return node.childNodes[lastStep].theGObject;
            }
            else if (targetType == 'C')
            {
                if ((bool) !node.controllers?.ContainsKey(lastStep))
                {
                    Log.Error($"not found controller '{path}' in '{rootName}'");
                    return null;
                }

                return node.controllers[lastStep];
            }
            else //if (targetType == 'T')
            {
                if ((bool) !node.transitions?.ContainsKey(lastStep))
                {
                    Log.Error($"not found transition '{path}' in '{rootName}'");
                    return null;
                }

                return node.transitions[lastStep];
            }
        }

        public void Bind(object target)
        {
            //NOTE: bind operation is one-time.
            if (_boundTarget != null)
            {
                Log.Error($"can not bind ui com repeatedly");
                return;
            }

            if (target == null)
            {
                Log.Error($"try to bind ui com to a null target");
                return;
            }
            if (_rootNode == null)
            {
                return;
            }

            Type targetName = target.GetType();

            UIBindEntry[] entries = UIBindMap.instance.GetEntries(targetName);
            foreach (UIBindEntry entry in entries)
            {
                string[] steps = entry.pathSteps;

                UIGObjectNode node = AdvanceToNode(steps, steps.Length - 1);
                if (node == null)
                {
                    Log.Error($"not found '{entry.searchPath}' in '{_rootNode.theGObjectName}'");
                    continue;
                }

                switch (entry.bindType)
                {
                case UIBindType.MemberField : BindMemberField (entry, node, target); break;
                case UIBindType.ButtonClick : BindButtonClick (entry, node, target); break;
                case UIBindType.ItemProvider: BindItemProvider(entry, node, target); break;
                case UIBindType.ItemRenderer: BindItemRenderer(entry, node, target); break;
                }
            }
        }

        private void BindMemberField(UIBindEntry entry, UIGObjectNode parent, object target)
        {
            string lastStep = entry.pathSteps[entry.pathSteps.Length - 1];
            if ((bool) !parent.childNodes?.ContainsKey(lastStep))
            {
                Log.Error($"not found '{entry.searchPath}' in '{_rootNode.theGObjectName}'");
                return;
            }

            UIGObjectNode lastNode    = parent.childNodes[lastStep];
            GObject       valueObject = lastNode.theGObject;
            Type          valueType   = valueObject.GetType();

            if (!entry.fieldInfo.FieldType.IsAssignableFrom(valueType))
            {
                string clazz = target.GetType().Name;
                string field = entry.fieldInfo.Name;

                Log.Error($"can not assign '{clazz}.{field}' with a '{valueType.Name}'");
                return;
            }

            entry.fieldInfo.SetValue(target, valueObject);
        }

        private void BindButtonClick(UIBindEntry entry, UIGObjectNode node, object target)
        {
            GButton button = node.theGObject.asButton;
            if (button == null)
            {
                Log.Error($"'{entry.searchPath}' from '{node.theGObjectName}' is not a button");
                return;
            }

            button.onClick.Set(() =>
            {
                entry.methodInfo.Invoke(target, null);
            });
        }

        private void BindItemProvider(UIBindEntry entry, UIGObjectNode node, object target)
        {
            GList list = node.theGObject.asList;
            if (list == null)
            {
                Log.Error($"'{entry.searchPath}' from '{node.theGObjectName}' is not a list");
                return;
            }

            list.itemProvider = (int index) =>
            {
                object[] args = new object[] { index };
                return entry.methodInfo.Invoke(target, args) as string;
            };
        }

        private void BindItemRenderer(UIBindEntry entry, UIGObjectNode node, object target)
        {
            GList list = node.theGObject.asList;
            if (list == null)
            {
                Log.Error($"'{entry.searchPath}' from '{node.theGObjectName}' is not a list");
                return;
            }

            list.itemRenderer = (int index, GObject item) =>
            {
                object[] args = new object[] { index, item };
                entry.methodInfo.Invoke(target, args);
            };
        }
    }
}

#endif
