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
using System.Security.Cryptography;

namespace NECS.Harness.Services
{
    public class ConstantService : IService
    {
        public static ConstantService instance => SGT.Get<ConstantService>();

        public ConcurrentDictionaryEx<string, ConfigObj> ConstantDB = new ConcurrentDictionaryEx<string, ConfigObj>();
        public Dictionary<long, List<ConfigObj>> TemplateInterestDB = new Dictionary<long, List<ConfigObj>>(); //TemplateAccessor Id
        public Dictionary<long, EntityTemplate> AllTemplates = new Dictionary<long, EntityTemplate>();
        public List<byte> loadedConfigFile = new List<byte>();
        public long checkedConfigVersion = 0;
        private long hashConfig = 0;
        public bool Loaded = false;

        public void PreInitialize()
        {
            
        }

        public void Initialize()
        {
            lock(this)
            {
                if (Loaded)
                    return;
                var gameConfDirectory = GlobalProgramState.instance.ConfigDir + "GameConfig";

                if (checkedConfigVersion != hashConfig)
                {
                    Logger.Log("Constant service update config");
                    if (Directory.Exists(GlobalProgramState.instance.ConfigDir))
                        Directory.Delete(GlobalProgramState.instance.ConfigDir, true);
                    Directory.CreateDirectory(GlobalProgramState.instance.ConfigDir);
                    File.WriteAllBytes(GlobalProgramState.instance.ConfigDir + "zippedconfig.zip", loadedConfigFile.ToArray());
                    ZipExt.DecompressToDirectory(loadedConfigFile.ToArray(), GlobalProgramState.instance.ConfigDir, (info) => { });
                }
                #region initload
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
                        }
                        var libname = nowLib.Replace(Directory.GetParent(nowLib).FullName, "").Replace(GlobalProgramState.instance.PathSystemSeparator, "");
                        if (nowObject.Deserialized == null)
                        {
                            nowObject.Path = file.Replace(gameConfDirectory, "").Replace(GlobalProgramState.instance.PathSystemSeparator + Path.GetFileName(file), "") + GlobalProgramState.instance.PathSeparator + Path.GetFileNameWithoutExtension(file);
                            nowObject.Path = nowObject.Path.Substring(1).Replace(GlobalProgramState.instance.PathSystemSeparator, GlobalProgramState.instance.PathSeparator);
                        }
                        else
                        {
                            nowObject.Path = file.Replace(gameConfDirectory, "").Replace(GlobalProgramState.instance.PathSystemSeparator + Path.GetFileName(file), "").Substring(1).Replace(GlobalProgramState.instance.PathSystemSeparator, GlobalProgramState.instance.PathSeparator);
                        }
                        nowObject.LibTree = new Lib() { LibName = libname, Path = nowObject.Path };
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
                #endregion
                Loaded = true;
            }
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

        public ConfigObj[] GetByLibName(string libName)
        {
            List<ConfigObj> result = new List<ConfigObj>();
            foreach (ConfigObj configObj in ConstantDB.Values)
            {
                if (configObj.LibTree.LibName == libName)
                    result.Add(configObj);
            }
            return result.ToArray();
        }
        public ConfigObj[] GetByHeadLibName(string libName)
        {
            List<ConfigObj> result = new List<ConfigObj>();
            foreach (ConfigObj configObj in ConstantDB.Values)
            {
                if (configObj.LibTree.HeadLib.LibName == libName)
                    result.Add(configObj);
            }
            return result.ToArray();
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

        public override void InitializeProcess()
        {

        }

        public override void OnDestroyReaction()
        {

        }

        public override void PostInitializeProcess()
        {

        }
    }

    public class ConfigObj
    {
        public long Id;
        public string Path;
        public Lib LibTree;
        public JObject Deserialized = null;

        public T GetObject<T>(string path)
        {
            return GetObjectImpl<T>(this.Deserialized, path);
        }

        protected T GetObjectImpl<T>(JObject storage, string path)
        {
            var pathSplit = path.Split(GlobalProgramState.instance.PathSeparator);
            var nowStorage = storage[pathSplit[0]];
            for (int i = 1; i < pathSplit.Length; i++)
            {
                if (!Lambda.TryExecute(() => nowStorage = nowStorage[pathSplit[i]]))
                    if (!Lambda.TryExecute(() => nowStorage = nowStorage[int.Parse(pathSplit[i])]))
                        throw new Exception("Wrong JObject iterator");
            }
            return nowStorage.ToObject<T>();
        }
    }

    public class Lib
    {
        public string Path;
        public string LibName;
        public Lib HeadLib
        {
            get
            {
                var splitedPath = this.Path.Split('\\');
                var newPath = "";
                for (int i = 0; i < splitedPath.Length - 1; i++)
                {
                    newPath += splitedPath[i] + "\\";
                }
                newPath = newPath.Substring(0, newPath.Length - 1);
                var newNameLib = splitedPath.ElementAt(splitedPath.Length - 2);
                return new Lib()
                {
                    Path = newPath,
                    LibName = newNameLib
                };
                //bmark: append nulllib wrapper for catching error headlib 
            }
        }
    }
}
