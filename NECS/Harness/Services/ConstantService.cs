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
using System.Security.Principal;
using System.IO;
using static NECS.Harness.Services.NetworkingService;
using NECS.ECS.DefaultObjects.Events.LowLevelNetEvent.ConfigEvent;

namespace NECS.Harness.Services
{
    public
#if GODOT4_0_OR_GREATER
    partial
#endif
    class ConstantService : IService
    {
        private static ConstantService cacheInstance;
        public static ConstantService instance {
            get
            {
                if(cacheInstance == null)
                    cacheInstance = SGT.Get<ConstantService>();
                return cacheInstance;
            }
        }

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
                var gameConfDirectory = config_path == "" ? GlobalProgramState.instance.GameConfigDir : config_path;

                gameConfDirectory = gameConfDirectory.Replace("\\", GlobalProgramState.instance.PathSystemSeparator).Replace("/", GlobalProgramState.instance.PathSystemSeparator);

                if (!Directory.Exists(gameConfDirectory))
                {
                    Directory.CreateDirectory(gameConfDirectory);
                }

                if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Client && checkedConfigVersion != hashConfig && config_path == "")
                {
                    NLogger.Log("Constant service update config");

                    var gamedatapath = Directory.GetParent(GlobalProgramState.instance.GameConfigDir).FullName;

                    if (Directory.Exists(GlobalProgramState.instance.GameConfigDir))
                        Directory.Delete(GlobalProgramState.instance.GameConfigDir, true);
                    File.WriteAllBytes(Path.Combine(gamedatapath, "zippedconfig.zip"), loadedConfigFile.ToArray());

                    var unzipFolder = Path.Combine(gamedatapath, "Unzipped");
                    if (Directory.Exists(unzipFolder))
                        Directory.Delete(unzipFolder, true);
                    Directory.CreateDirectory(unzipFolder);
                    ZipExt.DecompressToDirectory(loadedConfigFile.ToArray(), unzipFolder, (info) => { });
                    var movingDir = Directory.EnumerateDirectories(unzipFolder).OrderBy(x => x.Length).First();
                    Directory.Move(movingDir, GlobalProgramState.instance.GameConfigDir);
                }
                if(config_path != "")
                {
                    if(!File.Exists(Path.Combine(gameConfDirectory, "baseconfig.json")))
                    {
                        File.WriteAllText(Path.Combine(gameConfDirectory, "baseconfig.json"), JsonUtil.JsonPrettify(GlobalProgramState.instance.BaseConfigDefault));
                    }
                    if(GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Client)
                    {
                        if (!File.Exists(Path.Combine(gameConfDirectory, "loginconfig.json")))
                        {
                            File.WriteAllText(Path.Combine(gameConfDirectory, "loginconfig.json"), JsonUtil.JsonPrettify(GlobalProgramState.instance.BaseLoginConfig));
                        }
                    }
                }
                #region initload
                var nowLib = "";
                Dictionary<string, List<string>> Libs = new Dictionary<string, List<string>>();
                List<string> LibFiles = new List<string>();
                foreach (var file in GetRecursFiles(gameConfDirectory))
                {
                    var fileextension = Path.GetExtension(file);
                    if (fileextension.Contains(".yml") || fileextension.Contains(".json") || fileextension.Contains(".yaml"))
                    {
                        if (nowLib == "")
                        {
                            nowLib = Path.GetDirectoryName(file);
                        }
                        if (nowLib != Path.GetDirectoryName(file))
                        {
                            Libs.Add(nowLib, LibFiles);
                            LibFiles = new List<string>();
                            nowLib = Path.GetDirectoryName(file);
                        }
                        if (nowLib == Path.GetDirectoryName(file))
                        {
                            LibFiles.Add(file.Replace("\\", GlobalProgramState.instance.PathSystemSeparator).Replace("/", GlobalProgramState.instance.PathSystemSeparator));
                        }
                    }
                }
                if(LibFiles.Count > 0)
                {
                    Libs.Add(nowLib, LibFiles);
                    LibFiles = new List<string>();
                }
                foreach (var libfiles in Libs)
                {
                    nowLib = libfiles.Key;
                    foreach (var file in libfiles.Value)
                    {
                        ConfigObj nowObject = new ConfigObj();
                        var input = new StreamReader(file);
                        var yaml = new YamlDotNet.Serialization.Deserializer();
                        string jsonText = "";

                        if (file.Contains(".yml") || file.Contains(".yaml"))
                        {
                            var yamlObject = yaml.Deserialize<ExpandoObject>(input);
                            Newtonsoft.Json.JsonSerializer js = new Newtonsoft.Json.JsonSerializer();
                            var w = new StringWriter();
                            js.Serialize(w, yamlObject);
                            jsonText = w.ToString();
                        }
                        if (file.Contains(".json"))
                        {
                            jsonText = input.ReadToEnd();
                        }

                        System.IO.MemoryStream mStream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonText));
                        var reader = new JsonTextReader(new StreamReader(mStream));
                        var jObject = JObject.Load(reader);
                        switch (Path.GetFileNameWithoutExtension(file))
                        {
                            default:
                                nowObject.Deserialized = jObject;
                                break;
                        }

                        var libname = nowLib.Replace(Directory.GetParent(nowLib).FullName, "").Replace(GlobalProgramState.instance.PathSystemSeparator, "");

                        if (libfiles.Value.Count() == 1)
                        {
                            if(nowLib == gameConfDirectory)
                            {
                                nowObject.Path = file.Replace(gameConfDirectory, "").Replace(Path.GetFileName(file), "").Substring(1).Replace(GlobalProgramState.instance.PathSystemSeparator, GlobalProgramState.instance.PathSeparator) + Path.GetFileNameWithoutExtension(file);
                            }
                            else
                                nowObject.Path = nowLib.Replace(gameConfDirectory, "").Substring(1).Replace(GlobalProgramState.instance.PathSystemSeparator, GlobalProgramState.instance.PathSeparator);

                            #region wtf
                            //if (nowObject.Deserialized == null)
                            //{
                            //    nowObject.Path = file.Replace(gameConfDirectory, "").Replace(GlobalProgramState.instance.PathSystemSeparator + Path.GetFileName(file), "") + GlobalProgramState.instance.PathSeparator + Path.GetFileNameWithoutExtension(file);
                            //    nowObject.Path = nowObject.Path.Substring(1).Replace(GlobalProgramState.instance.PathSystemSeparator, GlobalProgramState.instance.PathSeparator);
                            //}
                            #endregion
                        }
                        if(libfiles.Value.Count() > 1)
                        {
                            nowObject.Path = file.Replace(gameConfDirectory, "").Replace(Path.GetFileName(file), "").Substring(1).Replace(GlobalProgramState.instance.PathSystemSeparator, GlobalProgramState.instance.PathSeparator) + Path.GetFileNameWithoutExtension(file);
                        }
                        nowObject.LibTree = new Lib() { LibName = libname, Path = nowObject.Path };
                        nowObject.RealPath = file;
                        if (nowObject.Path != null)
                            ConstantDB[nowObject.Path] = nowObject;
                    }
                }

                #endregion


                #region server pack

                if(GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Server && config_path == "")
                {
                    if (!File.Exists(GlobalProgramState.instance.GameDataDir + "zippedconfig.zip"))
                    {
                        #region prepareZipTemp

                        var ziptempfolder = Path.Combine(GlobalProgramState.instance.GameDataDir, "ZipTemp");
                        var ziptempgamedir = Path.Combine(ziptempfolder, GlobalProgramState.instance.GameConfigDir.Split(GlobalProgramState.instance.PathSystemSeparator[0]).Last());

                        if (Directory.Exists(ziptempfolder))
                            Directory.Delete(ziptempfolder, true);
                        FileEx.CopyFilesRecursively(new DirectoryInfo(GlobalProgramState.instance.GameConfigDir), new DirectoryInfo(ziptempgamedir));
                        #endregion
                        if (!Directory.Exists(ziptempfolder))
                        {
                            Directory.CreateDirectory(ziptempfolder);
                        }
                        ZipExt.CompressDirectory(ziptempfolder, Path.Combine(GlobalProgramState.instance.GameDataDir, "zippedconfig.zip"), (prog) => { });
                    }
                    Byte[] bytes = File.ReadAllBytes(Path.Combine(GlobalProgramState.instance.GameDataDir, "zippedconfig.zip"));
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
                    TemplateSetup();
                    Loaded = true;
                }
                #endregion

                #region client
                if(GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Client && config_path == "")
                {
                    TemplateSetup();
                    Loaded = true;
                    CustomSetupInitialized = false;
                    ServiceInitialized = true;
                    initializedCallbackCache();
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
                templateAccessor.TemplateInitialize();
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
                try
                {
                    if (configObj.LibTree.LibName == libName)
                        result.Add(configObj);
                }
                catch(Exception e)
                {
                    if(Defines.HiddenKeyNotFoundLog)
                        NLogger.LogError(e);
                }
            }
            return result.ToArray();
        }
        public ConfigObj[] GetByHeadLibName(string libName)
        {
            List<ConfigObj> result = new List<ConfigObj>();
            libName = libName.Replace(GlobalProgramState.instance.PathAltSeparator, GlobalProgramState.instance.PathSeparator);
            foreach (ConfigObj configObj in ConstantDB.Values)
            {
                try
                {
                    if (configObj.LibTree.HeadLib.LibName == libName)
                        result.Add(configObj);
                }
                catch (Exception e)
                {
                    if (Defines.HiddenKeyNotFoundLog)
                        NLogger.LogError(e);
                }
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
                    ls.Add("Folder: " + folder);
                    ls.AddRange(GetRecursFiles(folder));
                }
                string[] files = Directory.GetFiles(start_path);
                foreach (string filename in files)
                {
                    ls.Add(filename.Replace("\\", GlobalProgramState.instance.PathSystemSeparator).Replace("/", GlobalProgramState.instance.PathSystemSeparator));
                }
            }
            catch (Exception e)
            {
                if (Defines.HiddenKeyNotFoundLog)
                    NLogger.LogError(e);
            }
            return ls;
        }

        public override void InitializeProcess()
        {
            if(GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Client)
            {
                hashConfig = 0;
                byte[] configFile = null;

                if (File.Exists(Path.Combine(Directory.GetParent(GlobalProgramState.instance.GameConfigDir).FullName, "zippedconfig.zip")))
                {
                    configFile = File.ReadAllBytes(Path.Combine(Directory.GetParent(GlobalProgramState.instance.GameConfigDir).FullName, "zippedconfig.zip"));
                    hashConfig = BitConverter.ToInt64(MD5.Create().ComputeHash(configFile), 0);
                }
                
                CustomSetupInitialized = true;
                Action<Network.NetworkModels.SocketAdapter> socketAction = (Network.NetworkModels.SocketAdapter socketAdapter) => {
                    if (!Loaded)
                    {
                        ManagerScope.instance.eventManager.OnEventAdd(new ConfigCheckEvent()
                        {
                            configHash = hashConfig
                        });
                    }
                };
                if (NetworkingService.instance.SocketAdapters.Count() == 0)
                    NetworkingService.instance.OnConnectExternal += new SocketHandler(socketAction);
                else
                {
                    socketAction(NetworkingService.instance.ClientSocket);
                }
            }
            if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Server)
                SetupConfigs();
        }

        public override void OnDestroyReaction()
        {

        }

        /// <summary>
        /// for normal service setup
        /// </summary>
        public override void PostInitializeProcess()
        {

        }
    }

    public class ConfigObj
    {
        public long Id;
        public string Path;
        public string RealPath;
        public string JSONRepresentation => Deserialized.ToString(Formatting.None);
        public string SerializedData;//for json
        public Lib LibTree;
        public JObject Deserialized = null;

        public void UpdateOnDisk()
        {
            var fileextension = System.IO.Path.GetExtension(RealPath);
            if (fileextension.Contains(".yml") || fileextension.Contains(".yaml"))
            {
                //var input = new StreamReader(file);
                //var yaml = new YamlDotNet.Serialization.Serializer();
                //yaml.Serialize(file, input);
            }
            if(fileextension.Contains(".json"))
            {
                File.WriteAllText(RealPath, JSONRepresentation);
            }
        }

        public T GetObject<T>(string path)
        {
            return this.Deserialized.GetObjectByPath<T>(path);
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
