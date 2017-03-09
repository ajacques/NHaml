namespace Haml.Compiling
{
    internal interface ITemplateRenderer
    {
        void Write(string content);
        void Write(char content);
        void Write(string format, params object[] formats);
        void CallMethod(string name);
        void ConditionalBegin();
        void ConditionalElseBegin();
        void ConditionalElseEnd();
        void ConditionalEnd(string methodName);
    }
}
