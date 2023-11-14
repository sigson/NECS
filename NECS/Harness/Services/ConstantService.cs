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

        //server
        public List<byte> ConfigFilesZip = new List<byte>();
        public long hashConfigFilesZip;

        public void PreInitialize()
        {
            
        }
        /// <summary>
        /// can be running from another location, example for tech config loading
        /// </summary>
        /// <param name="config_path">path to techical config, like socket info</param>
        public void SetupConfigs(string config_path = "")
        {
            lock(this)
            {
                if (Loaded)
                    return;
                var gameConfDirectory = config_path == "" ? GlobalProgramState.instance.ConfigDir : config_path;

                if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Client && checkedConfigVersion != hashConfig && config_path == "")
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
                    if (file.Contains(".yml") || file.Contains(".json"))
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
                        string jsonText = "";

                        if (file.Contains(".yml"))
                        {
                            var yamlObject = yaml.Deserialize<ExpandoObject>(input);
                            Newtonsoft.Json.JsonSerializer js = new Newtonsoft.Json.JsonSerializer();
                            var w = new StringWriter();
                            js.Serialize(w, yamlObject);
                            jsonText = w.ToString();
                        }
                        if (file.Contains(".json"))
                        {
                            jsonText = input.ToString();
                        }
                        
                        System.IO.MemoryStream mStream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonText));
                        var reader = new JsonTextReader(new StreamReader(mStream));
                        var jObject = JObject.Load(reader);

                        switch (Path.GetFileNameWithoutExtension(file))
                        {
                            case "id":
                                nowObject.Id = jObject.GetValue("id").Value<long>();
                                break;
                            default:
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
                    else
                    {

                    }
                }
                ConstantDB[nowObject.Path] = nowObject;

                #endregion


                #region server pack

                if(GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Server && config_path == "")
                {
                    if (!File.Exists(GlobalProgramState.instance.ConfigDir + "zippedconfig.zip"))
                    {
                        #region prepareZipTemp
                        if (Directory.Exists(GlobalProgramState.instance.ConfigDir + "ZipTemp"))
                            Directory.Delete(GlobalProgramState.instance.ConfigDir + "ZipTemp", true);
                        FileEx.CopyFilesRecursively(new DirectoryInfo(GlobalProgramState.instance.ConfigDir + "GameConfig"), new DirectoryInfo(GlobalProgramState.instance.ConfigDir + "ZipTemp" + GlobalProgramState.instance.PathSystemSeparator + "GameConfig"));
                        File.Copy(GlobalProgramState.instance.ConfigDir + "donateshop.json", GlobalProgramState.instance.ConfigDir + "ZipTemp" + GlobalProgramState.instance.PathSystemSeparator + "donateshop.json");
                        File.Copy(GlobalProgramState.instance.ConfigDir + "garageshop.json", GlobalProgramState.instance.ConfigDir + "ZipTemp" + GlobalProgramState.instance.PathSystemSeparator + "garageshop.json");
                        File.Copy(GlobalProgramState.instance.ConfigDir + "selectablemapdb.json", GlobalProgramState.instance.ConfigDir + "ZipTemp" + GlobalProgramState.instance.PathSystemSeparator + "selectablemapdb.json");
                        #endregion
                        ZipExt.CompressDirectory(GlobalProgramState.instance.ConfigDir + "ZipTemp", GlobalProgramState.instance.ConfigDir + "zippedconfig.zip", (prog) => { });
                    }
                    Byte[] bytes = File.ReadAllBytes(GlobalProgramState.instance.ConfigDir + "zippedconfig.zip");
                    using (MD5CryptoServiceProvider CSP = new MD5CryptoServiceProvider())
                    {
                        var byteHash = CSP.ComputeHash(bytes);
                        string result = "";
                        foreach (byte b in byteHash)
                            result += b.ToString();
                        hashConfigFilesZip = long.Parse(result.Substring(0, 18));
                    }
                    hashConfigFilesZip = BitConverter.ToInt64(MD5.Create().ComputeHash(bytes), 0);
                    ConfigFilesZip = new List<byte>(bytes);
                }
                #endregion
            }
        }

        private void TemplateSetup()
        {
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
                TemplateInterestDB[templateAccessor.GetId()] = interestedList;
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
            if (ConstantDB.TryGetValue(path.Replace(GlobalProgramState.instance.PathAltSeparator, GlobalProgramState.instance.PathSeparator), out obj))
                return obj;
            return null;
        }

        public ConfigObj[] GetByLibName(string libName)
        {
            List<ConfigObj> result = new List<ConfigObj>();
            libName = libName.Replace(GlobalProgramState.instance.PathAltSeparator, GlobalProgramState.instance.PathSeparator);
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
            libName = libName.Replace(GlobalProgramState.instance.PathAltSeparator, GlobalProgramState.instance.PathSeparator);
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
            start_path = start_path.Replace(GlobalProgramState.instance.PathAltSeparator, GlobalProgramState.instance.PathSeparator);
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

        /// <summary>
        /// for normal service setup
        /// </summary>
        public override void PostInitializeProcess()
        {
            TaskEx.RunAsync(() => {
                if(GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Client)
                {
                    while(hashConfig == 0)
                    {
                        Task.Delay(100).Wait();
                    }
                }
                SetupConfigs();
                TemplateSetup();
                Loaded = true;
            });
            
        }
    }

    public class ConfigObj
    {
        public long Id;
        public string Path;
        public string SerializedData;//for json
        public Lib LibTree;
        public JObject Deserialized = null;

        public T GetObject<T>(string path)
        {
            return GetObjectImpl<T>(this.Deserialized, path.Replace(GlobalProgramState.instance.PathAltSeparator, GlobalProgramState.instance.PathSeparator));
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
