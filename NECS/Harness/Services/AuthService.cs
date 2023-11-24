using NECS.Core.Logging;
using NECS.ECS.DefaultObjects.Events.LowLevelNetEvent.Auth;
using NECS.ECS.ECSCore;
using NECS.Harness.Model;
using NECS.Network.NetworkModels;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.Harness.Services
{
    public class AuthService : IService
    {
        public Action<UserDataRowBase> AuthorizationRealization = null;
        public Func<ClientRegistrationEvent, UserDataRowBase> SetupAuthorizationRealization = null;
        private static AuthService cacheInstance;
        public static AuthService instance
        {
            get
            {
                if (cacheInstance == null)
                    cacheInstance = SGT.Get<AuthService>();
                return cacheInstance;
            }
        }
        private ConcurrentDictionary<SocketAdapter, ECSEntity> SocketToEntity = new ConcurrentDictionary<SocketAdapter, ECSEntity>();
        private ConcurrentDictionary<ECSEntity, SocketAdapter> EntityToSocket = new ConcurrentDictionary<ECSEntity, SocketAdapter>();

        public void AuthProcess(ClientAuthEvent clientAuthEvent)
        {
            if(DBService.instance.DBProvider.LoginCheck(clientAuthEvent.Username, HashExtension.MD5(clientAuthEvent.Password)))
            {
                var userdata = DBService.instance.DBProvider.GetUserViaCallsign<UserDataRowBase>(clientAuthEvent.Username);
                if(AuthorizationRealization != null)
                {
                    AuthorizationRealization(userdata);
                }
                else
                {
                    Logger.Error("Not initialized AuthService.instance.AuthorizationRealization method");
                }
            }
        }

        public void RegistrationProcess(ClientRegistrationEvent clientAuthEvent)
        {
            if (DBService.instance.DBProvider.UsernameAvailable(clientAuthEvent.Username) &&
                DBService.instance.DBProvider.EmailAvailable(clientAuthEvent.Email))
            {
                if (SetupAuthorizationRealization != null)
                {
                    var userdata = DBService.instance.DBProvider.CreateUser<UserDataRowBase>(SetupAuthorizationRealization(clientAuthEvent));
                    if (AuthorizationRealization != null)
                    {
                        AuthorizationRealization(userdata);
                    }
                    else
                    {
                        Logger.Error("Not initialized AuthService.instance.AuthorizationRealization method");
                    }
                }
                else
                {
                    Logger.Error("Not initialized AuthService.instance.SetupAuthorizationRealization method");
                }
            }
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
}
