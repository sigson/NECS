using NECS.ECS.ECSCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.ECS.DefaultObjects.ECSComponents
{
    [System.Serializable]
    [TypeUid(28)]
    public class UserEmailComponent : ECSComponent
    {
        static public new long Id { get; set; }
        static public new System.Collections.Generic.List<System.Action> StaticOnChangeHandlers { get; set; }
         public UserEmailComponent() { }
		 public UserEmailComponent(string email, bool emailVerified)
		 {
			 this.Email = email;
			 this.EmailVerified = emailVerified;
		 }

		 public string Email;
		 public bool EmailVerified;
    }
}
