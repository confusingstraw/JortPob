using System.Threading;

namespace JortPob.Worker
{
    public abstract class Worker
    {
        public bool IsDone { get; protected set; }
        protected Thread _thread { get; set; }
        public int ExitCode { get; set; }
        public string ErrorMessage { get; set; }
    }
}
