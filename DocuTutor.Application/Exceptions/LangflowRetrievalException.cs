namespace DocuTutor.Application.Exceptions
{
    public class LangflowRetrievalException : Exception
    {
        public LangflowRetrievalException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}
