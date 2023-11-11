using NECS.Core.Logging;
using NECS.ECS.ECSCore;
using NECS.Extensions;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NECS.Harness.Model;

namespace NECS.Harness.Services
{
    public class ConstantService : IService
    {
        public ConcurrentDictionaryEx<string, ConfigObj> ConstantDB = new ConcurrentDictionaryEx<string, ConfigObj>();
        public Dictionary<long, List<ConfigObj>> TemplateInterestDB = new Dictionary<long, List<ConfigObj>>(); //TemplateAccessor Id
        public Dictionary<long, EntityTemplate> AllTemplates = new Dictionary<long, EntityTemplate>();
        private void Initialize()
        {
            var gameConfDirectory = Path.Join(GlobalProgramState.instance.ConfigDir, "GameConfig");
            var nowLib = "";
            ConfigObj nowObject = new ConfigObj();
            foreach (var file in GetRecursFiles(gameConfDirectory))
            {
                if (file.Contains(".yml"))
                {
                    if (nowLib == "")
                    {
                        nowLib = Path.GetDirectoryName(file);
                        nowObject = new ConfigObj();
                    }
                    else if (nowLib != Path.GetDirectoryName(file))
                    {

                        ConstantDB[nowObject.Path] = nowObject;
                        nowLib = Path.GetDirectoryName(file);
                        nowObject = new ConfigObj();
                    }

                    var input = new StreamReader(file);
                    var yaml = new YamlDotNet.Serialization.Deserializer();
                    var yamlObject = yaml.Deserialize<ExpandoObject>(input);
                    Newtonsoft.Json.JsonSerializer js = new Newtonsoft.Json.JsonSerializer();
                    var w = new StringWriter();
                    js.Serialize(w, yamlObject);
                    string jsonText = w.ToString();
                    System.IO.MemoryStream mStream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonText));
                    var reader = new JsonTextReader(new StreamReader(mStream));
                    var jObject = JObject.Load(reader);

                    switch (Path.GetFileNameWithoutExtension(file))
                    {
                        case "id":
                            nowObject.Id = jObject.GetValue("id").Value<long>();
                            break;
                        case "public":
                            nowObject.Deserialized = jObject;
                            break;
                        case "public_en":
                            nowObject.DeserializedInfo = jObject;
                            break;
                    }
                    nowObject.LibName = nowLib.Replace(Directory.GetParent(nowLib).FullName, "").Replace(GlobalProgramState.instance.PathSeparator, "");
                    nowObject.HeadLibName = Directory.GetParent(nowLib).Name;
                    nowObject.Path = file.Replace(gameConfDirectory, "").Replace(GlobalProgramState.instance.PathSeparator + Path.GetFileName(file), "").Substring(1).Replace("/", "\\");
                }
            }
            ConstantDB[nowObject.Path] = nowObject;

            var allTemplates = ECSAssemblyExtensions.GetAllSubclassOf(typeof(EntityTemplate)).Select(x => (EntityTemplate)Activator.CreateInstance(x)).ToList(); //load interested configs to template accessor db
            foreach (EntityTemplate templateAccessor in allTemplates)
            {
                List<ConfigObj> interestedList = new List<ConfigObj>();
                AllTemplates[templateAccessor.GetId()] = templateAccessor;
                foreach (var path in templateAccessor.ConfigPath)
                {
                    var result = ConstantDB.Values.Where(x => x.Path == path).FirstOrDefault();
                    if (result != null)
                        interestedList.Add(result);
                }
                try
                {
                    var field = templateAccessor.GetType().GetField("<Id>k__BackingField", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                    var customAttrib = templateAccessor.GetType().GetCustomAttribute<TypeUidAttribute>();
                    if (customAttrib != null)
                        field.SetValue(null, customAttrib.Id);
                    //Console.WriteLine(comp.GetId().ToString() + "  " + comp.GetType().Name);
                }
                catch
                {
                    Console.WriteLine(templateAccessor.GetType().Name);
                }
                TemplateInterestDB.Add(templateAccessor.GetId(), interestedList);
            }
            Logger.Log("Constant service initialized");
        }

        public List<ConfigObj> GetByTemplate(EntityTemplate templateAccessor)
        {
            List<ConfigObj> result = new List<ConfigObj>();
            if (TemplateInterestDB.TryGetValue(templateAccessor.GetId(), out result))
            {
                return result;
            }
            return result;
        }

        public ConfigObj GetByConfigPath(string path)
        {
            ConfigObj obj;
            if (ConstantDB.TryGetValue(path, out obj))
                return obj;
            return null;
        }

        public ConfigObj GetByLibName(string libName)
        {
            foreach (ConfigObj configObj in ConstantDB.Values)
            {
                if (configObj.LibName == libName)
                    return configObj;
            }
            return null;
        }
        public List<string> GetRecursFiles(string start_path)
        {
            List<string> ls = new List<string>();
            try
            {
                string[] folders = Directory.GetDirectories(start_path);
                foreach (string folder in folders)
                {
                    ls.Add("Папка: " + folder);
                    ls.AddRange(GetRecursFiles(folder));
                }
                string[] files = Directory.GetFiles(start_path);
                foreach (string filename in files)
                {
                    ls.Add(filename);
                }
            }
            catch (System.Exception e)
            {
            }
            return ls;
        }

        public override void PostInitializeProcess()
        {
            
        }

        public override void InitializeProcess()
        {
            Initialize();
        }

        public override void OnDestroyReaction()
        {
            
        }
    }

    public class ConfigObj
    {
        public long Id;
        public string Path;
        public string LibName;
        public string HeadLibName;
        public JObject Deserialized;
        public JObject DeserializedInfo;
    }
}
