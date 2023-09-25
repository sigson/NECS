using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.Harness.Model
{
    public interface IProxyBehaviour
    {
        public void ExecuteInstruction(Action<object> action, object Object, string ErrorLog = "", Action exceptionCallback = null);

        public void ExecuteInstruction(Action action, string ErrorLog = "", Action exceptionCallback = null);

        public T ExecuteFunction<T>(Func<T> function, string ErrorLog = "", Action exceptionCallback = null) where T : class;
    }
}
