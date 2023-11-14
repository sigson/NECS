using NECS.Harness.Model;
using NECS.Harness.Services;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace UTanksServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Just Tanks Server";
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(@"
      _   _____     ____    _____   ____   __     __  _____   ____  
     | | |_   _|   / ___|  | ____| |  _ \  \ \   / / | ____| |  _ \ 
  _  | |   | |     \___ \  |  _|   | |_) |  \ \ / /  |  _|   | |_) |
 | |_| |   | |      ___) | | |___  |  _ <    \ V /   | |___  |  _ < 
  \___/    |_|     |____/  |_____| |_| \_\    \_/    |_____| |_| \_\
                                                                    
");
            Console.ResetColor();

            IService.RegisterAllServices();
            GlobalProgramState.instance.ProgramType = GlobalProgramState.ProgramTypeEnum.Client;
            ConstantService.instance.SetupConfigs(GlobalProgramState.instance.TechConfigDir);
            IService.RegisterAllServices();

            #region ClearScreen
            Func<Task> asyncUpd = async () =>
            {
                await Task.Run(() => {
                    Thread.Sleep(5000);
                    Console.Clear();
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine(@"
      _   _____     ____    _____   ____   __     __  _____   ____  
     | | |_   _|   / ___|  | ____| |  _ \  \ \   / / | ____| |  _ \ 
  _  | |   | |     \___ \  |  _|   | |_) |  \ \ / /  |  _|   | |_) |
 | |_| |   | |      ___) | | |___  |  _ <    \ V /   | |___  |  _ < 
  \___/    |_|     |____/  |_____| |_| \_\    \_/    |_____| |_| \_\
                                                                    
");
                    Console.ResetColor();
                });
            };
            asyncUpd();
            #endregion
        }
    }
}