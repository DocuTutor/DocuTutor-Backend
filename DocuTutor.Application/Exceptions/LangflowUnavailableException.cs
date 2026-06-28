namespace DocuTutor.Application.Exceptions
{
    public class LangflowUnavailableException : Exception
    {
        public LangflowUnavailableException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}
