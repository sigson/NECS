using NECS.ECS.ECSCore;
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
            IService.InitializeService(ECSService.instance);
            IService.InitializeAllServices();

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
            RunInput(args);
        }

        static async void RunInput(string[] args)
        {
            while (true)
            {
                string[] input = CreateArgs(Console.ReadLine());
                if (input.Length == 0) input = new string[] { string.Empty };
                if (args.Length > 0)
                {

                }
                switch (input[0].ToLower())
                {
                    case "exit":
                        return;
                    case "jtserver":
                        if (input.Length >= 2)
                        {
                            switch (input[1].ToLower())
                            {
                                case "kill":
                                case "exit":
                                case "start":
                                case "restart":
                                default:
                                    Console.WriteLine($"Unknown command '{input[1]}', to view a list of options, use 'JTServer'");
                                    break;
                            }
                        }
                        else
                            Console.WriteLine("JTServer Commands:" +
                                "\n  kill - Stops the server (alias: exit, stop)" +
                                "\n  start - Starts the server (alias: run)" +
                                "\n  enable - Enable the excecution of the server (session-only)" +
                                "\n  disable - Disabled the server and kills the process (if any, session-only)"
                            );
                        break;
                    case "help":
                        Console.WriteLine("Useful Commands" +
                            "\n  addserver - Create login credentials to the DB" +
                            "\n  ld - loadbots" +
                            "\n  lda - loadbotsall" +
                            "\n  lda - loadbotsall" +
                            "\n  tb - testbattle" +
                            "\n  JTServer - Modify the JTServer process");
                        break;
                    case "drops":
                        Console.WriteLine("not realized");
                        break;
                    default:
                        Console.WriteLine($"Unknown Command '{input[0]}'");
                        break;
                }
            }
        }

        public static string[] CreateArgs(string commandLine)
        {
            StringBuilder argsBuilder = new StringBuilder(commandLine);
            bool inQuote = false;

            // Convert the spaces to a newline sign so we can split at newline later on
            // Only convert spaces which are outside the boundries of quoted text
            for (int i = 0; i < argsBuilder.Length; i++)
            {
                if (argsBuilder[i].Equals('"'))
                {
                    inQuote = !inQuote;
                }

                if (argsBuilder[i].Equals(' ') && !inQuote)
                {
                    argsBuilder[i] = '\n';
                }
            }

            // Split to args array
            string[] args = argsBuilder.ToString().Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // Clean the '"' signs from the args as needed.
            for (int i = 0; i < args.Length; i++)
            {
                args[i] = ClearQuotes(args[i]);
            }

            return args;
        }
        static string ClearQuotes(string stringWithQuotes)
        {
            int quoteIndex;
            if ((quoteIndex = stringWithQuotes.IndexOf('"')) == -1)
            {
                // String is without quotes..
                return stringWithQuotes;
            }

            // Linear sb scan is faster than string assignemnt if quote count is 2 or more (=always)
            StringBuilder sb = new StringBuilder(stringWithQuotes);
            for (int i = quoteIndex; i < sb.Length; i++)
            {
                if (sb[i].Equals('"'))
                {
                    // If we are not at the last index and the next one is '"', we need to jump one to preserve one
                    if (i != sb.Length - 1 && sb[i + 1].Equals('"'))
                    {
                        i++;
                    }

                    // We remove and then set index one backwards.
                    // This is because the remove itself is going to shift everything left by 1.
                    sb.Remove(i--, 1);
                }
            }

            return sb.ToString();
        }

    }
}