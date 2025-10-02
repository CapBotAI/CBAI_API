using System;

namespace App.BLL.Services
{
    /// <summary>
    /// Thrown when the AI provider indicates quota limits or too-many-requests and the client
    /// should stop issuing further requests.
    /// </summary>
    public class AIQuotaExceededException : Exception
    {
        public AIQuotaExceededException() { }
        public AIQuotaExceededException(string message) : base(message) { }
        public AIQuotaExceededException(string message, Exception inner) : base(message, inner) { }
    }
}
