using NECS.Core.Logging;
using NECS.ECS.DefaultObjects.ECSComponents;
using NECS.ECS.DefaultObjects.Events.ECSEvents;
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
        public Func<UserDataRowBase, ECSEntity> AuthorizationRealization = null;
        public Func<ClientRegistrationEvent, UserDataRowBase> SetupAuthorizationRealization = null;
        private static AuthService cacheInstance;
        #region client
        public string LastSendedUsername;
        public string LastSendedPassword;
        #endregion
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
                    AuthorizationProcess(userdata, NetworkingService.instance.SocketAdapters[clientAuthEvent.SocketSourceId]);
                }
                else
                {
                    NLogger.Error("Not initialized AuthService.instance.AuthorizationRealization method");
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
                        AuthorizationProcess(userdata, NetworkingService.instance.SocketAdapters[clientAuthEvent.SocketSourceId]);
                    }
                    else
                    {
                        NLogger.Error("Not initialized AuthService.instance.AuthorizationRealization method");
                    }
                }
                else
                {
                    NLogger.Error("Not initialized AuthService.instance.SetupAuthorizationRealization method");
                }
            }
        }

        private void AuthorizationProcess(UserDataRowBase userData, SocketAdapter socketAdapter)
        {
            var entity = SocketToEntity.Values.Where(x => x.GetComponent<UsernameComponent>().Username == userData.Username).FirstOrDefault();
            var userLogged = new UserLoggedEvent();
            if(entity == null)
            {
                entity = AuthorizationRealization(userData);
                entity.AddComponentSilent(new SocketComponent() { Socket = socketAdapter  });
                SocketToEntity[socketAdapter] = entity;
                EntityToSocket[entity] = socketAdapter;
                ManagerScope.instance.entityManager.OnAddNewEntity(entity);
                userLogged.userRelogin = false;
            }
            else
            {
                var oldsocket = entity.GetComponent<SocketComponent>().Socket;
                SocketToEntity[socketAdapter] = entity;
                EntityToSocket[entity] = socketAdapter;
                SocketToEntity.Remove(oldsocket, out _);
                entity.GetComponent<SocketComponent>().Socket = socketAdapter;
                userLogged.userRelogin = true;
            }
            userLogged.Username = userData.Username;
            userLogged.userEntity = entity;
            userLogged.userEntityId = entity.instanceId;
            ManagerScope.instance.eventManager.OnEventAdd(userLogged);
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
