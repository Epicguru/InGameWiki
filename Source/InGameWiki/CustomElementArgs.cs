namespace InGameWiki
{
    public class CustomElementArgs
    {
        public readonly string Input;
        public readonly WikiPage Page;

        internal CustomElementArgs(WikiPage page, string input)
        {
            this.Page = page;
            this.Input = input;
        }
    }
}
