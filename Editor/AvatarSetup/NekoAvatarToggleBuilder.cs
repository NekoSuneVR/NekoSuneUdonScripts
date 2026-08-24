using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace NekoSune.Avatars.Editor
{
    [NekoAddon(Order = 12)]
    internal sealed class NekoAvatarToggleBuilderAddon : INekoAddon
    {
        public string Id { get { return "toggle-builder"; } }
        public string TitleKey { get { return "Toggle + Menu Builder"; } }
        public string DescriptionKey { get { return "Create a beginner avatar toggle with parameter, menu control, animations and FX layer."; } }
        public string CategoryKey { get { return "cat.avatar"; } }
        public string Glyph { get { return "T"; } }
        public bool IsAvailable { get { return true; } }
        public void Open() { NekoAvatarToggleBuilderWindow.Open(); }
    }

    internal sealed class NekoAvatarToggleBuilderWindow : EditorWindow
    {
        GameObject _avatar;
        GameObject _target;
        string _parameter = "NekoToggle";
        string _label = "Toggle";
        bool _defaultOn;
        bool _saved = true;
        bool _synced = true;
        string _folder = "Assets/NekoSune/AvatarSetup";
        string _status = "Choose an avatar and target object.";

        [MenuItem("NekoSune/Avatar/Toggle + Menu Builder", false, 18)]
        public static void Open()
        {
            var w = GetWindow<NekoAvatarToggleBuilderWindow>(false, "Toggle + Menu Builder", true);
            w.minSize = new Vector2(520f, 430f);
            w.Show();
        }

        void OnGUI()
        {
            NekoStyles.Ensure();
            NekoStyles.Header("Toggle + Menu Builder", "Beginner setup for Parameters → Menu → FX → Animation");
            _avatar=(GameObject)EditorGUILayout.ObjectField("Avatar root",_avatar,typeof(GameObject),true);
            _target=(GameObject)EditorGUILayout.ObjectField("Object to toggle",_target,typeof(GameObject),true);
            _label=EditorGUILayout.TextField("Menu label",_label);
            _parameter=EditorGUILayout.TextField("Parameter",_parameter);
            _defaultOn=EditorGUILayout.Toggle("Default ON",_defaultOn);
            _saved=EditorGUILayout.Toggle("Saved",_saved);
            _synced=EditorGUILayout.Toggle("Synced",_synced);
            _folder=EditorGUILayout.TextField("Output folder",_folder);

            EditorGUILayout.HelpBox("Build creates OFF/ON animation clips, a Bool Animator parameter + FX layer, a VRChat Expression Parameter and a Toggle menu control. Existing assets are reused when possible.",MessageType.Info);
            if(GUILayout.Button("BUILD TOGGLE + MENU + FX",NekoStyles.PrimaryButton,GUILayout.Height(36f)))Build();
            EditorGUILayout.HelpBox(_status,MessageType.None);

            EditorGUILayout.Space(6f);
            GUILayout.Label("Beginner map",EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Expression Menu toggle\n      ↓\nExpression Parameter (Bool)\n      ↓\nFX Animator parameter\n      ↓\nOFF ↔ ON states\n      ↓\nAnimationClip changes the selected object",EditorStyles.wordWrappedLabel);
        }

        void Build()
        {
            if(_avatar==null||_target==null){_status="Assign the avatar root and target object.";return;}
            if(!_target.transform.IsChildOf(_avatar.transform)&&_target!=_avatar){_status="Target must be inside the avatar hierarchy.";return;}
            string parameter=SafeName(_parameter); if(string.IsNullOrEmpty(parameter)){_status="Parameter name is empty.";return;}

            Type descriptorType=FindType("VRC.SDK3.Avatars.Components.VRCAvatarDescriptor");
            Type paramsType=FindType("VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters");
            Type menuType=FindType("VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu");
            if(descriptorType==null||paramsType==null||menuType==null){_status="VRChat Avatars SDK is not installed in this project. Avatar Tools itself stays SDK-optional so World-only dependencies remain clean.";return;}
            Component descriptor=_avatar.GetComponent(descriptorType); if(descriptor==null){_status="The selected root has no VRCAvatarDescriptor.";return;}

            EnsureFolder(_folder);
            string path=AnimationUtility.CalculateTransformPath(_target.transform,_avatar.transform);
            AnimationClip off=CreateActiveClip(path,false,parameter+"_OFF.anim");
            AnimationClip on=CreateActiveClip(path,true,parameter+"_ON.anim");

            AnimatorController fx=FindFxController(descriptor);
            if(fx==null)
            {
                string fxPath=AssetDatabase.GenerateUniqueAssetPath(_folder.TrimEnd('/')+"/NekoFX.controller");
                fx=AnimatorController.CreateAnimatorControllerAtPath(fxPath);
                TryAssignFxController(descriptor,fx);
            }
            AddFxToggle(fx,parameter,off,on,_defaultOn);

            ScriptableObject parameters=GetOrCreateAsset(descriptor,"expressionParameters",paramsType,"NekoExpressionParameters.asset");
            ScriptableObject menu=GetOrCreateAsset(descriptor,"expressionsMenu",menuType,"NekoExpressionsMenu.asset");
            if(parameters==null||menu==null){_status="Could not create/read VRChat expression assets for this SDK version.";return;}
            AddExpressionParameter(parameters,parameter,_defaultOn,_saved,_synced);
            bool menuOk=AddMenuToggle(menu,_label,parameter);
            SetMember(descriptor,"customExpressions",true);
            EditorUtility.SetDirty(descriptor);EditorUtility.SetDirty(parameters);EditorUtility.SetDirty(menu);EditorUtility.SetDirty(fx);AssetDatabase.SaveAssets();
            _status=menuOk?"Built toggle '"+_label+"' using parameter '"+parameter+"'.":"FX/parameter built, but the selected root Expressions Menu already has 8 controls. Add a submenu or free a slot, then run again.";
        }

        AnimationClip CreateActiveClip(string path,bool active,string file)
        {
            AnimationClip clip=new AnimationClip();clip.frameRate=60f;clip.name=Path.GetFileNameWithoutExtension(file);
            EditorCurveBinding binding=EditorCurveBinding.FloatCurve(path,typeof(GameObject),"m_IsActive");AnimationCurve curve=AnimationCurve.Constant(0f,1f,active?1f:0f);AnimationUtility.SetEditorCurve(clip,binding,curve);
            string asset=AssetDatabase.GenerateUniqueAssetPath(_folder.TrimEnd('/')+"/"+file);AssetDatabase.CreateAsset(clip,asset);return clip;
        }

        static void AddFxToggle(AnimatorController controller,string parameter,AnimationClip off,AnimationClip on,bool defaultOn)
        {
            bool exists=false;foreach(AnimatorControllerParameter p in controller.parameters)if(p.name==parameter){exists=true;break;}if(!exists)controller.AddParameter(parameter,AnimatorControllerParameterType.Bool);
            var sm=new AnimatorStateMachine();sm.name=parameter+" Toggle";AssetDatabase.AddObjectToAsset(sm,controller);
            AnimatorState offState=sm.AddState("OFF");offState.motion=off;AnimatorState onState=sm.AddState("ON");onState.motion=on;sm.defaultState=defaultOn?onState:offState;
            AnimatorStateTransition toOn=offState.AddTransition(onState);toOn.hasExitTime=false;toOn.duration=0f;toOn.AddCondition(AnimatorConditionMode.If,0f,parameter);
            AnimatorStateTransition toOff=onState.AddTransition(offState);toOff.hasExitTime=false;toOff.duration=0f;toOff.AddCondition(AnimatorConditionMode.IfNot,0f,parameter);
            AnimatorControllerLayer layer=new AnimatorControllerLayer{name="Neko/"+parameter,defaultWeight=1f,stateMachine=sm};controller.AddLayer(layer);
        }

        static AnimatorController FindFxController(Component descriptor)
        {
            object layers=GetMember(descriptor,"baseAnimationLayers");Array a=layers as Array;if(a==null)return null;
            for(int i=0;i<a.Length;i++)
            {
                object layer=a.GetValue(i);object type=GetMember(layer,"type");if(type==null)continue;
                if(type.ToString().IndexOf("FX",StringComparison.OrdinalIgnoreCase)<0)continue;
                return GetMember(layer,"animatorController") as AnimatorController;
            }
            return null;
        }

        static void TryAssignFxController(Component descriptor,AnimatorController controller)
        {
            object layersObj=GetMember(descriptor,"baseAnimationLayers");Array layers=layersObj as Array;if(layers==null)return;
            for(int i=0;i<layers.Length;i++)
            {
                object layer=layers.GetValue(i);object type=GetMember(layer,"type");if(type==null||type.ToString().IndexOf("FX",StringComparison.OrdinalIgnoreCase)<0)continue;
                SetMember(layer,"isDefault",false);SetMember(layer,"animatorController",controller);layers.SetValue(layer,i);SetMember(descriptor,"baseAnimationLayers",layers);return;
            }
        }

        ScriptableObject GetOrCreateAsset(Component descriptor,string field,Type type,string file)
        {
            object existing=GetMember(descriptor,field);ScriptableObject so=existing as ScriptableObject;if(so!=null)return so;
            so=ScriptableObject.CreateInstance(type);string path=AssetDatabase.GenerateUniqueAssetPath(_folder.TrimEnd('/')+"/"+file);AssetDatabase.CreateAsset(so,path);SetMember(descriptor,field,so);return so;
        }

        static void AddExpressionParameter(ScriptableObject asset,string name,bool defaultOn,bool saved,bool synced)
        {
            FieldInfo f=asset.GetType().GetField("parameters",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);if(f==null)return;Array old=f.GetValue(asset) as Array;Type element=f.FieldType.GetElementType();if(element==null)return;
            int count=old==null?0:old.Length;for(int i=0;i<count;i++){object p=old.GetValue(i);if((GetMember(p,"name") as string)==name)return;}
            Array next=Array.CreateInstance(element,count+1);if(old!=null)Array.Copy(old,next,count);object entry=Activator.CreateInstance(element);SetMember(entry,"name",name);SetEnumMember(entry,"valueType","Bool");SetMember(entry,"defaultValue",defaultOn?1f:0f);SetMember(entry,"saved",saved);SetMember(entry,"networkSynced",synced);next.SetValue(entry,count);f.SetValue(asset,next);
        }

        static bool AddMenuToggle(ScriptableObject menu,string label,string parameter)
        {
            FieldInfo f=menu.GetType().GetField("controls",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);if(f==null)return false;IList list=f.GetValue(menu) as IList;if(list==null)return false;
            for(int i=0;i<list.Count;i++){object c=list[i];if((GetMember(c,"name") as string)==label)return true;}
            if(list.Count>=8)return false;
            Type controlType=list.GetType().IsGenericType?list.GetType().GetGenericArguments()[0]:menu.GetType().GetNestedType("Control",BindingFlags.Public|BindingFlags.NonPublic);if(controlType==null)return false;
            object control=Activator.CreateInstance(controlType);SetMember(control,"name",label);SetEnumMember(control,"type","Toggle");SetMember(control,"value",1f);
            FieldInfo pf=controlType.GetField("parameter",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);if(pf!=null){object p=Activator.CreateInstance(pf.FieldType);SetMember(p,"name",parameter);pf.SetValue(control,p);}list.Add(control);return true;
        }

        static object GetMember(object o,string name){if(o==null)return null;Type t=o.GetType();FieldInfo f=t.GetField(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);if(f!=null)return f.GetValue(o);PropertyInfo p=t.GetProperty(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);return p!=null&&p.CanRead?p.GetValue(o,null):null;}
        static void SetMember(object o,string name,object value){if(o==null)return;Type t=o.GetType();FieldInfo f=t.GetField(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);if(f!=null){try{f.SetValue(o,ConvertValue(value,f.FieldType));}catch{}return;}PropertyInfo p=t.GetProperty(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);if(p!=null&&p.CanWrite)try{p.SetValue(o,ConvertValue(value,p.PropertyType),null);}catch{}}
        static void SetEnumMember(object o,string name,string enumName){if(o==null)return;Type t=o.GetType();FieldInfo f=t.GetField(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);if(f!=null&&f.FieldType.IsEnum)try{f.SetValue(o,Enum.Parse(f.FieldType,enumName,true));}catch{} }
        static object ConvertValue(object v,Type target){if(v==null)return null;if(target.IsInstanceOfType(v))return v;try{return Convert.ChangeType(v,target,CultureInfo.InvariantCulture);}catch{return v;}}
        static Type FindType(string full){foreach(Assembly a in AppDomain.CurrentDomain.GetAssemblies()){Type t=a.GetType(full,false);if(t!=null)return t;}return null;}
        static string SafeName(string s){if(string.IsNullOrEmpty(s))return "";char[] a=s.ToCharArray();for(int i=0;i<a.Length;i++)if(!(char.IsLetterOrDigit(a[i])||a[i]=='_'||a[i]=='/'))a[i]='_';return new string(a).Trim('/');}
        static void EnsureFolder(string path){path=path.Replace('\\','/').TrimEnd('/');string[]p=path.Split('/');if(p.Length==0||p[0]!="Assets")return;string cur="Assets";for(int i=1;i<p.Length;i++){string n=cur+"/"+p[i];if(!AssetDatabase.IsValidFolder(n))AssetDatabase.CreateFolder(cur,p[i]);cur=n;}}
    }
}
